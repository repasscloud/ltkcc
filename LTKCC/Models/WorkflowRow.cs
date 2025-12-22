using SQLite;

namespace LTKCC.Models;

[Table("Workflows")]
public sealed class WorkflowRow
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public string Name { get; set; } = "";

    // Increment when you publish a new definition of the same workflow template.
    [Indexed]
    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    // Hydrated manually (not stored in Workflows table).
    [Ignore]
    public List<WorkflowStepRow> Steps { get; set; } = new();
}
