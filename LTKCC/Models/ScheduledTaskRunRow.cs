using SQLite;

namespace LTKCC.Models;

[Table("ScheduledTaskRuns")]
public sealed class ScheduledTaskRunRow
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public Guid ScheduledTaskId { get; set; }

    [Indexed]
    public Guid WorkflowId { get; set; }

    [Indexed]
    public int WorkflowVersion { get; set; }

    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedUtc { get; set; }

    // "running" | "succeeded" | "failed" | "cancelled"
    [Indexed]
    public string Status { get; set; } = "running";

    public string? Error { get; set; }
}
