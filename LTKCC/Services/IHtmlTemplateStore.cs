namespace LTKCC.Services;

public interface IHtmlTemplateStore
{
    Task<IReadOnlyList<string>> ListHtmlFilesAsync(CancellationToken ct = default);
    Task<string> ReadAllTextAsync(string fileName, CancellationToken ct = default);

    // NEW: import a file the user picked into Templates dir
    Task<string> ImportAsync(string fileName, Stream source, bool overwrite, CancellationToken ct = default);
}
