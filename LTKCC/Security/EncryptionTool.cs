using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace LTKCC.Security;

public static class EncryptionTool
{
    // Hard-coded PSK (pre-shared key). Replace with your own.
    // Keep it stable across app versions if you need to decrypt old values.
    private const string Psk = "b0lngzGIGPC1gYmW3lDk1zsIIVbWuEee-3-cyCKh0cBVdEVrJZ7pS-vwQzMOww3c";

    // Token format:
    // - v2: enc:v2:<base64url(payload)>
    //   payload = [1 byte version][4 bytes saltLen][salt][16 bytes iv][32 bytes mac][ciphertext]
    //
    // Back-compat (optional): v1 (AES-GCM)
    // - v1: enc:v1:<base64url(payload)>
    //   payload = [1 byte version][4 bytes saltLen][salt][12 bytes nonce][16 bytes tag][ciphertext]

    private const byte VersionV2 = 2;
    private const string PrefixV2 = "enc:v2:";

    private const byte VersionV1 = 1;
    private const string PrefixV1 = "enc:v1:";

    public static string Encrypt(string plaintext)
    {
        if (plaintext is null) throw new ArgumentNullException(nameof(plaintext));

        // v2 is used on all platforms to avoid per-platform crypto support issues (e.g., MacCatalyst AesGcm).
        // If you have existing v1 tokens, Decrypt() will still accept them on platforms that support it.

        byte[] salt = RandomBytes(16);

        // Derive 64 bytes from PSK + salt: first 32 bytes = encKey, next 32 bytes = macKey
        DeriveKeysV2(Psk, salt, out var encKey, out var macKey);

        byte[] iv = RandomBytes(16); // AES block size
        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);

        byte[] cipherBytes = EncryptAesCbc(encKey, iv, plainBytes);

        // MAC over: [version][saltLen][salt][iv][cipher]
        byte[] header = BuildV2Header(VersionV2, salt, iv);
        byte[] mac = ComputeHmacSha256(macKey, header, cipherBytes);

        // Build payload: [version][saltLen(4)][salt][iv(16)][mac(32)][cipher]
        int payloadLen = header.Length + mac.Length + cipherBytes.Length;
        byte[] payload = new byte[payloadLen];

        int o = 0;
        Buffer.BlockCopy(header, 0, payload, o, header.Length);
        o += header.Length;

        Buffer.BlockCopy(mac, 0, payload, o, mac.Length);
        o += mac.Length;

        Buffer.BlockCopy(cipherBytes, 0, payload, o, cipherBytes.Length);

        CryptographicOperations.ZeroMemory(encKey);
        CryptographicOperations.ZeroMemory(macKey);

        return PrefixV2 + Base64UrlEncode(payload);
    }

    public static string Decrypt(string token)
    {
        if (token is null) throw new ArgumentNullException(nameof(token));

        if (token.StartsWith(PrefixV2, StringComparison.Ordinal))
            return DecryptV2(token);

        if (token.StartsWith(PrefixV1, StringComparison.Ordinal))
            return DecryptV1(token);

        throw new FormatException("Token does not start with an expected prefix (enc:v1: or enc:v2:).");
    }

    // ----------------------
    // v2 (AES-CBC + HMAC-SHA256)
    // ----------------------

    private static string DecryptV2(string token)
    {
        byte[] payload = Base64UrlDecode(token.Substring(PrefixV2.Length));
        if (payload.Length < 1 + 4 + 16 + 32) throw new FormatException("Token payload too short.");

        int o = 0;
        byte ver = payload[o++];
        if (ver != VersionV2) throw new NotSupportedException($"Unsupported version: {ver}");

        int saltLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(o, 4));
        o += 4;

        if (saltLen < 0 || saltLen > 1024) throw new FormatException("Invalid salt length.");
        int minLen = 1 + 4 + saltLen + 16 + 32;
        if (payload.Length < minLen) throw new FormatException("Token payload too short.");

        byte[] salt = new byte[saltLen];
        if (saltLen > 0)
        {
            Buffer.BlockCopy(payload, o, salt, 0, saltLen);
            o += saltLen;
        }

        byte[] iv = new byte[16];
        Buffer.BlockCopy(payload, o, iv, 0, iv.Length);
        o += iv.Length;

        byte[] mac = new byte[32];
        Buffer.BlockCopy(payload, o, mac, 0, mac.Length);
        o += mac.Length;

        int cipherLen = payload.Length - o;
        if (cipherLen < 0) throw new FormatException("Invalid cipher length.");
        byte[] cipherBytes = new byte[cipherLen];
        Buffer.BlockCopy(payload, o, cipherBytes, 0, cipherLen);

        DeriveKeysV2(Psk, salt, out var encKey, out var macKey);

        // Verify MAC before decrypt
        byte[] header = BuildV2Header(ver, salt, iv);
        byte[] expectedMac = ComputeHmacSha256(macKey, header, cipherBytes);

        bool ok = CryptographicOperations.FixedTimeEquals(mac, expectedMac);
        CryptographicOperations.ZeroMemory(expectedMac);

        if (!ok)
        {
            CryptographicOperations.ZeroMemory(encKey);
            CryptographicOperations.ZeroMemory(macKey);
            throw new CryptographicException("Decryption failed (bad key or corrupted token).");
        }

        byte[] plainBytes;
        try
        {
            plainBytes = DecryptAesCbc(encKey, iv, cipherBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encKey);
            CryptographicOperations.ZeroMemory(macKey);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static void DeriveKeysV2(string psk, byte[] salt, out byte[] encKey, out byte[] macKey)
    {
        const int iterations = 100_000;

        using var kdf = new Rfc2898DeriveBytes(
            password: psk,
            salt: salt ?? Array.Empty<byte>(),
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256);

        byte[] material = kdf.GetBytes(64);

        encKey = new byte[32];
        macKey = new byte[32];
        Buffer.BlockCopy(material, 0, encKey, 0, 32);
        Buffer.BlockCopy(material, 32, macKey, 0, 32);
        CryptographicOperations.ZeroMemory(material);
    }

    private static byte[] BuildV2Header(byte version, byte[] salt, byte[] iv)
    {
        salt ??= Array.Empty<byte>();
        iv ??= Array.Empty<byte>();

        if (iv.Length != 16) throw new ArgumentException("IV must be 16 bytes for AES-CBC.", nameof(iv));

        // [version][saltLen(4)][salt][iv(16)]
        byte[] header = new byte[1 + 4 + salt.Length + 16];

        int o = 0;
        header[o++] = version;

        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(o, 4), salt.Length);
        o += 4;

        if (salt.Length > 0)
        {
            Buffer.BlockCopy(salt, 0, header, o, salt.Length);
            o += salt.Length;
        }

        Buffer.BlockCopy(iv, 0, header, o, 16);
        return header;
    }

    private static byte[] EncryptAesCbc(byte[] key, byte[] iv, byte[] plaintext)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var enc = aes.CreateEncryptor();
        return enc.TransformFinalBlock(plaintext, 0, plaintext.Length);
    }

    private static byte[] DecryptAesCbc(byte[] key, byte[] iv, byte[] ciphertext)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    private static byte[] ComputeHmacSha256(byte[] key, byte[] a, byte[] b)
    {
        using var h = new HMACSHA256(key);

        // Avoid concat allocations; HMAC has incremental API.
        h.TransformBlock(a, 0, a.Length, null, 0);
        h.TransformFinalBlock(b, 0, b.Length);

        return h.Hash ?? throw new CryptographicException("HMAC computation failed.");
    }

    // ----------------------
    // v1 (AES-GCM) back-compat
    // ----------------------

    private static string DecryptV1(string token)
    {
#if MACCATALYST || NET8_0_MACCATALYST
        // AesGcm is not supported on MacCatalyst, so v1 tokens cannot be decrypted there.
        // If you need this, migrate stored tokens to v2 on a platform that can read v1.
        throw new PlatformNotSupportedException("enc:v1: tokens are not supported on MacCatalyst. Re-encrypt using v2.");
#else
        byte[] payload = Base64UrlDecode(token.Substring(PrefixV1.Length));
        if (payload.Length < 1 + 4) throw new FormatException("Token payload too short.");

        int o = 0;
        byte ver = payload[o++];
        if (ver != VersionV1) throw new NotSupportedException($"Unsupported version: {ver}");

        int saltLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(o, 4));
        o += 4;

        if (saltLen < 0 || saltLen > 1024) throw new FormatException("Invalid salt length.");
        if (payload.Length < 1 + 4 + saltLen + 12 + 16) throw new FormatException("Token payload too short.");

        byte[] salt = new byte[saltLen];
        if (saltLen > 0)
        {
            Buffer.BlockCopy(payload, o, salt, 0, saltLen);
            o += saltLen;
        }

        byte[] nonce = new byte[12];
        Buffer.BlockCopy(payload, o, nonce, 0, nonce.Length);
        o += nonce.Length;

        byte[] tag = new byte[16];
        Buffer.BlockCopy(payload, o, tag, 0, tag.Length);
        o += tag.Length;

        int cipherLen = payload.Length - o;
        if (cipherLen < 0) throw new FormatException("Invalid cipher length.");

        byte[] cipherBytes = new byte[cipherLen];
        Buffer.BlockCopy(payload, o, cipherBytes, 0, cipherLen);

        byte[] key = DeriveKeyV1(Psk, salt);
        byte[] plainBytes = new byte[cipherBytes.Length];
        byte[] aad = BuildAadV1(ver, salt);

        try
        {
            using var aes = new AesGcm(key, tagSizeInBytes: tag.Length);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes, aad);
        }
        catch (CryptographicException ex)
        {
            throw new CryptographicException("Decryption failed (bad key or corrupted token).", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        return Encoding.UTF8.GetString(plainBytes);
#endif
    }

#if !(MACCATALYST || NET8_0_MACCATALYST)
    private static byte[] DeriveKeyV1(string psk, byte[] salt)
    {
        const int iterations = 100_000;

        using var kdf = new Rfc2898DeriveBytes(
            password: psk,
            salt: salt ?? Array.Empty<byte>(),
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256);

        return kdf.GetBytes(32);
    }

    private static byte[] BuildAadV1(byte version, byte[] salt)
    {
        salt ??= Array.Empty<byte>();
        var aad = new byte[1 + salt.Length];
        aad[0] = version;
        if (salt.Length > 0) Buffer.BlockCopy(salt, 0, aad, 1, salt.Length);
        return aad;
    }
#endif

    // ----------------------
    // Shared utilities
    // ----------------------

    private static byte[] RandomBytes(int length)
    {
        byte[] b = new byte[length];
        RandomNumberGenerator.Fill(b);
        return b;
    }

    // Base64Url (RFC 4648 §5) helpers: no padding, URL-safe chars
    private static string Base64UrlEncode(byte[] data)
    {
        string b64 = Convert.ToBase64String(data);
        return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static byte[] Base64UrlDecode(string s)
    {
        string b64 = s.Replace('-', '+').Replace('_', '/');
        int pad = b64.Length % 4;
        if (pad != 0) b64 = b64.PadRight(b64.Length + (4 - pad), '=');
        return Convert.FromBase64String(b64);
    }
}
