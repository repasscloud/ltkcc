// File: Services/ITemplateFileService.cs
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LTKCC.Services;

public interface ITemplateFileService
{
    Task<IReadOnlyList<string>> ListHtmlFilesAsync();
    Task<string> ReadAsync(string fileName);
    Task WriteAsync(string fileName, string content, bool overwrite);

    // REQUIRED for the Add button copy/import
    Task ImportAsync(string fileName, Stream source, bool overwrite);

    Task DeleteAsync(string fileName);
    Task RenameAsync(string oldFileName, string newFileName, bool overwrite);
}
