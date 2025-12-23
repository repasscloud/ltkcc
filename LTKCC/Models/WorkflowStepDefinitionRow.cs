using SQLite;

namespace LTKCC.Models;

[Table("WorkflowStepDefinition")]
public sealed class WorkflowStepDefinitionRow
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Indexed(Unique = true)]
    public string Name { get; set; } = "";

    public string TemplateFileName { get; set; } = "";

    // JSON array of strings, e.g. ["APPLICATION","REF_ID",...]
    public string ParameterKeysJson { get; set; } = "[]";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
