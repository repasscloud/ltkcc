using SQLite;

namespace LTKCC.Models;

public sealed class Client
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Indexed(Unique = true)]
    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
