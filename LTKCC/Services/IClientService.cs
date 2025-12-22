using LTKCC.Models;

namespace LTKCC.Services;

public interface IClientService
{
    Task<IReadOnlyList<Client>> GetAllAsync();
    Task<Client?> GetByIdAsync(Guid id);

    /// <summary>
    /// Inserts or updates. Updates CreatedAt per app requirement.
    /// Enforces unique Name.
    /// </summary>
    Task<SaveClientResult> UpsertAsync(Client client);
}

public sealed record SaveClientResult(bool Ok, string? Error = null, Client? Saved = null);
