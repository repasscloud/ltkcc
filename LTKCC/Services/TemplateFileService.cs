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

        var files = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Where(n =>
                n.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                n.EndsWith(".htm", StringComparison.OrdinalIgnoreCase))
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

    // THIS is what the Add button needs (copy picked file into Templates dir)
    public async Task ImportAsync(string fileName, Stream source, bool overwrite)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        var safeName = NormalizeFileName(fileName);
        var fullPath = GetFullPathValidated(safeName);

        if (!overwrite && File.Exists(fullPath))
            throw new IOException("File already exists.");

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        if (source.CanSeek)
            source.Position = 0;

        await using var dest = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(dest).ConfigureAwait(false);
        await dest.FlushAsync().ConfigureAwait(false);
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

    private static string GetFullPathValidated(string fileName)
    {
        var safe = NormalizeFileName(fileName);

        var dir = AppPaths.GetTemplatesDir();
        Directory.CreateDirectory(dir);

        var fullPath = Path.GetFullPath(Path.Combine(dir, safe));
        var fullDir = Path.GetFullPath(dir);

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

        name = Path.GetFileName(name);

        var ext = Path.GetExtension(name);

        if (string.IsNullOrEmpty(ext))
        {
            name += ".html";
        }
        else if (ext.Equals(".htm", StringComparison.OrdinalIgnoreCase))
        {
            name = Path.ChangeExtension(name, ".html");
        }
        else if (!ext.Equals(".html", StringComparison.OrdinalIgnoreCase))
        {
            // keep original extension but ensure it ends with .html
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
