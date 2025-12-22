using SQLite;

namespace LTKCC.Models;

public sealed class HtmlTemplate
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public bool IsEnabled { get; set; } = true;
}