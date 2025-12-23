using Microsoft.Maui.Storage;

namespace LTKCC.Services;

public sealed class HtmlTemplateStore : IHtmlTemplateStore
{
    private readonly string _templatesDir;

    public HtmlTemplateStore()
    {
        _templatesDir = Path.Combine(FileSystem.AppDataDirectory, "Templates");
        Directory.CreateDirectory(_templatesDir);
    }

    public Task<IReadOnlyList<string>> ListHtmlFilesAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_templatesDir);

        var files = Directory.EnumerateFiles(_templatesDir, "*.html", SearchOption.TopDirectoryOnly)
                             .Concat(Directory.EnumerateFiles(_templatesDir, "*.htm", SearchOption.TopDirectoryOnly))
                             .Select(Path.GetFileName)
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                             .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files!);
    }

    public async Task<string> ReadAllTextAsync(string fileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        var safeName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(_templatesDir, safeName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Template not found.", safeName);

        using var fs = File.OpenRead(fullPath);
        using var sr = new StreamReader(fs);
        return await sr.ReadToEndAsync(ct);
    }

    public async Task<string> ImportAsync(string fileName, Stream source, bool overwrite, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        Directory.CreateDirectory(_templatesDir);

        var safeName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(_templatesDir, safeName);

        if (!overwrite && File.Exists(fullPath))
            throw new IOException("File already exists in Templates directory.");

        await using var dst = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(dst, ct);

        return safeName;
    }
}
