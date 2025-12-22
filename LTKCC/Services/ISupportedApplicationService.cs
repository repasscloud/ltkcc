using LTKCC.Models;

namespace LTKCC.Services;

public interface ISupportedApplicationService
{
    Task<IReadOnlyList<SupportedApplication>> GetAllAsync();
    Task<SupportedApplication?> GetByIdAsync(Guid id);
    Task<SaveSupportedApplicationResult> UpsertAsync(SupportedApplication app);
}

public sealed record SaveSupportedApplicationResult(bool Ok, string? Error = null, SupportedApplication? Saved = null);
