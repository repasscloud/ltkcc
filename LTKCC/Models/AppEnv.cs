using SQLite;

namespace LTKCC.Models;

public sealed class AppEnv
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public  string Environment { get; set; } = default!;
}