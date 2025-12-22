using LTKCC.Models.Static;
using SQLite;

namespace LTKCC.Models;

[Table("ScheduledTasks")]
public sealed class ScheduledTaskRow
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public string Name { get; set; } = "";

    [Indexed]
    public Guid WorkflowId { get; set; }

    // Pin a schedule to a specific workflow definition.
    [Indexed]
    public int WorkflowVersion { get; set; } = 1;

    public TriggerType TriggerType { get; set; } = TriggerType.Cron;      // "cron" | "interval" | "manual"
    public string TriggerDataJson { get; set; } = "";      // cron expr, interval seconds, etc.

    // Runtime parameters for the workflow (free-form JSON)
    public string ParametersJson { get; set; } = "";

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
