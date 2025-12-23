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
    Task DeleteAsync(string fileName);
    Task RenameAsync(string oldFileName, string newFileName, bool overwrite);

    // Used by "Add" (copy a picked file into Templates directory)
    Task ImportAsync(string fileName, Stream source, bool overwrite);
}
