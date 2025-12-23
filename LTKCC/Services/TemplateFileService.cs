// File: Services/TemplateFileService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LTKCC.Services;

public sealed class TemplateFileService : ITemplateFileService
{
    public Task<IReadOnlyList<string>> ListHtmlFilesAsync()
    {
        var dir = AppPaths.GetTemplatesDir();
        Directory.CreateDirectory(dir);

        // IMPORTANT: Path.GetFileName returns string? so force it to string to avoid CS8620.
        var files = Directory.EnumerateFiles(dir, "*.html", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)                         // string?
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)                                  // now string
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    public async Task<string> ReadAsync(string fileName)
    {
        var fullPath = GetFullPathValidated(fileName);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Template not found.", fileName);

        return await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
    }

    public async Task WriteAsync(string fileName, string content, bool overwrite)
    {
        var fullPath = GetFullPathValidated(fileName);

        if (!overwrite && File.Exists(fullPath))
            throw new IOException("File already exists.");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content ?? string.Empty).ConfigureAwait(false);
    }

    public Task DeleteAsync(string fileName)
    {
        var fullPath = GetFullPathValidated(fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task RenameAsync(string oldFileName, string newFileName, bool overwrite)
    {
        var oldPath = GetFullPathValidated(oldFileName);
        var newPath = GetFullPathValidated(newFileName);

        if (!File.Exists(oldPath))
            throw new FileNotFoundException("Template not found.", oldFileName);

        if (!overwrite && File.Exists(newPath))
            throw new IOException("Target file already exists.");

        Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);

        if (File.Exists(newPath))
            File.Delete(newPath);

        File.Move(oldPath, newPath);
        return Task.CompletedTask;
    }

    public async Task ImportAsync(string fileName, Stream source, bool overwrite)
    {
        var safeName = NormalizeFileName(fileName);
        var fullPath = GetFullPathValidated(safeName);

        if (!overwrite && File.Exists(fullPath))
            throw new IOException("File already exists.");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        if (source.CanSeek)
            source.Position = 0;

        await using var dest = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(dest).ConfigureAwait(false);
    }

    private static string GetFullPathValidated(string fileName)
    {
        var safe = NormalizeFileName(fileName);

        var dir = AppPaths.GetTemplatesDir();
        Directory.CreateDirectory(dir);

        var fullPath = Path.GetFullPath(Path.Combine(dir, safe));
        var fullDir = Path.GetFullPath(dir);

        // Block path traversal
        if (!fullPath.StartsWith(fullDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, fullDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid file name.");
        }

        return fullPath;
    }

    public static string NormalizeFileName(string? input)
    {
        var name = (input ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("File name is required.");

        // Keep only the file name (no paths)
        name = Path.GetFileName(name);

        // Normalize .htm -> .html
        if (name.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4] + ".html";
        }
        else if (!name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            name += ".html";
        }

        foreach (var ch in Path.GetInvalidFileNameChars())
        {
            if (name.Contains(ch))
                throw new InvalidOperationException("File name contains invalid characters.");
        }

#if WINDOWS
        var stem = Path.GetFileNameWithoutExtension(name);
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON","PRN","AUX","NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };
        if (reserved.Contains(stem))
            throw new InvalidOperationException("File name is reserved on Windows.");
#endif

        return name;
    }
}
