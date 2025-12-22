using System.Net.Mail;

namespace LTKCC.Validation;

public static class EmailValidator
{
    // Normalizes + validates. Throws if invalid.
    public static string NormalizeOrThrow(string value)
    {
        var v = (value ?? "").Trim();

        if (v.Length == 0)
            throw new ArgumentException("Email is empty.");

        try
        {
            // MailAddress is a solid pragmatic validator
            var addr = new MailAddress(v);

            // Reject display-name formats like "Bob <bob@x.com>"
            if (!string.Equals(addr.Address, v, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Email must be a plain address (no display name).");

            return addr.Address.Trim().ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw new ArgumentException($"Invalid email: '{v}'.");
        }
    }

    public static bool TryNormalize(string value, out string normalized)
    {
        try
        {
            normalized = NormalizeOrThrow(value);
            return true;
        }
        catch
        {
            normalized = "";
            return false;
        }
    }
}
