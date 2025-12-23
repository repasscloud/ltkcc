// File: Models/DistributionListRow.cs
using SQLite;

namespace LTKCC.Models;

[Table("DistributionList")]
public sealed class DistributionListRow
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = "";

    // Optional metadata (safe defaults)
    public long CreatedUtcTicks { get; set; } = DateTime.UtcNow.Ticks;
    public long UpdatedUtcTicks { get; set; } = DateTime.UtcNow.Ticks;
}
