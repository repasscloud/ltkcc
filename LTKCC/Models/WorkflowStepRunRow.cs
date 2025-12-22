using SQLite;

namespace LTKCC.Models;

[Table("WorkflowStepRuns")]
public sealed class WorkflowStepRunRow
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public Guid ScheduledTaskRunId { get; set; }

    public Guid WorkflowStepId { get; set; }
    public int StepOrder { get; set; }

    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedUtc { get; set; }

    // "running" | "succeeded" | "failed" | "skipped"
    [Indexed]
    public string Status { get; set; } = "running";

    public string? OutputJson { get; set; }
    public string? Error { get; set; }
}
