using SQLite;
using LTKCC.Models.Static;

namespace LTKCC.Models;

[Table("DistributionLists")]
public sealed class DistributionListRow
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public string Name { get; set; } = "";

    public DLType Type { get; set; } = DLType.To;
    public DLPrivacyType Privacy { get; set; } = DLPrivacyType.External;

    // Convenience only (not stored in DistributionLists table).
    // Stored via DistributionListEmailRow table instead.
    [Ignore]
    public List<string> Emails { get; set; } = new();
}
