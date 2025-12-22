using SQLite;

namespace LTKCC.Models;

[Table("WorkflowSteps")]
public sealed class WorkflowStepRow
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public Guid WorkflowId { get; set; }

    // This is what allows “pinning” scheduled tasks to a stable workflow definition.
    [Indexed]
    public int WorkflowVersion { get; set; }

    public int StepOrder { get; set; } // 1..N (you enforce this in code)

    public string Name { get; set; } = "";
    public string ActionType { get; set; } = "";      // e.g. "http", "email", "sql", "delay"
    public string ActionDataJson { get; set; } = "";  // JSON payload per ActionType

    public bool IsEnabled { get; set; } = true;
}
