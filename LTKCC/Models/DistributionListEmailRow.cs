// File: Models/DistributionListEmailRow.cs
using SQLite;

namespace LTKCC.Models;

[Table("DistributionListEmail")]
public sealed class DistributionListEmailRow
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int DistributionListId { get; set; }

    [MaxLength(320)]
    public string Email { get; set; } = "";

    [MaxLength(200)]
    public string DisplayName { get; set; } = "";
}
