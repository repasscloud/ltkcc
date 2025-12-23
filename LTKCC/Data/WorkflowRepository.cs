using SQLite;
using LTKCC.Models;

namespace LTKCC.Data;

public sealed class WorkflowRepository
{
    private readonly SQLiteAsyncConnection _db;

    public WorkflowRepository(AppDb appDb) => _db = appDb.Connection;

    public Task<List<WorkflowRow>> ListAsync(bool activeOnly = true)
    {
        var q = _db.Table<WorkflowRow>();
        if (activeOnly) q = q.Where(x => x.IsActive);
        return q.OrderBy(x => x.Name).ThenByDescending(x => x.Version).ToListAsync();
    }

    public async Task<WorkflowRow?> GetAsync(Guid workflowId, int version)
    {
        var wf = await _db.Table<WorkflowRow>()
            .Where(x => x.Id == workflowId && x.Version == version)
            .FirstOrDefaultAsync();

        if (wf is null) return null;

        wf.Steps = await _db.Table<WorkflowStepRow>()
            .Where(s => s.WorkflowId == workflowId && s.WorkflowVersion == version)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        return wf;
    }

    public async Task<int> GetLatestVersionAsync(Guid workflowId)
    {
        var latest = await _db.Table<WorkflowRow>()
            .Where(x => x.Id == workflowId)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync();

        return latest?.Version ?? 0;
    }

    /// <summary>
    /// Creates/publishes a new version of a workflow definition (immutable history).
    /// If workflow Id is empty, one is generated and Version = 1.
    /// </summary>
    public async Task<(Guid workflowId, int version)> PublishNewVersionAsync(
        Guid workflowId,
        string name,
        IReadOnlyList<WorkflowStepRow> steps,
        bool isActive = true)
    {
        if (workflowId == Guid.Empty)
            workflowId = Guid.NewGuid();

        var nextVersion = (await GetLatestVersionAsync(workflowId)) + 1;
        if (nextVersion <= 0) nextVersion = 1;

        var wf = new WorkflowRow
        {
            Id = workflowId,
            Name = name,
            Version = nextVersion,
            IsActive = isActive,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        // Normalize steps (ensure 1..N ordering, set ids)
        var normalized = steps
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.StepOrder)
            .Select((s, idx) =>
            {
                if (s.Id == Guid.Empty) s.Id = Guid.NewGuid();
                s.WorkflowId = workflowId;
                s.WorkflowVersion = nextVersion;
                s.StepOrder = idx + 1;
                return s;
            })
            .ToList();

        await _db.RunInTransactionAsync(tran =>
        {
            tran.Insert(wf);

            // Insert steps for that (WorkflowId, Version)
            foreach (var s in normalized)
                tran.Insert(s);
        });

        return (workflowId, nextVersion);
    }
}
