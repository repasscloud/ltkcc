using SQLite;

namespace LTKCC.Models;

[Table("DistributionListEmails")]
public sealed class DistributionListEmailRow
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public Guid DistributionListId { get; set; }

    // Stored normalized (lowercase, trimmed)
    [Indexed]
    public string Email { get; set; } = "";
}
