using SQLite;
using LTKCC.Models;

namespace LTKCC.Data;

public sealed class ScheduledTaskRepository
{
    private readonly SQLiteAsyncConnection _db;

    public ScheduledTaskRepository(AppDb appDb) => _db = appDb.Db;

    public Task<List<ScheduledTaskRow>> ListAsync(bool enabledOnly = false)
    {
        var q = _db.Table<ScheduledTaskRow>();
        if (enabledOnly) q = q.Where(x => x.IsEnabled);
        return q.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<ScheduledTaskRow?> GetAsync(Guid scheduledTaskId)
    {
        var obj = await _db.FindAsync<ScheduledTaskRow>(scheduledTaskId);
        return obj; // may be null
    }

    public async Task UpsertAsync(ScheduledTaskRow task)
    {
        if (task.Id == Guid.Empty)
        {
            task.Id = Guid.NewGuid();
            task.CreatedUtc = DateTime.UtcNow;
        }

        task.UpdatedUtc = DateTime.UtcNow;
        await _db.InsertOrReplaceAsync(task);
    }
}
