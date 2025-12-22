using SQLite;
using LTKCC.Data;
using LTKCC.Models;

namespace LTKCC.Services;

public sealed class SupportedApplicationService : ISupportedApplicationService
{
    private readonly AppDb _db;

    public SupportedApplicationService(AppDb db) => _db = db;

    public async Task<IReadOnlyList<SupportedApplication>> GetAllAsync()
    {
        await _db.InitAsync();
        return await _db.Connection
            .Table<SupportedApplication>()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<SupportedApplication?> GetByIdAsync(Guid id)
    {
        await _db.InitAsync();
        return await _db.Connection
            .Table<SupportedApplication>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<SaveSupportedApplicationResult> UpsertAsync(SupportedApplication app)
    {
        await _db.InitAsync();

        var name = (app.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return new SaveSupportedApplicationResult(false, "Name is required.");

        app.Name = name;
        app.Description = string.IsNullOrWhiteSpace(app.Description) ? null : app.Description.Trim();

        // Case-insensitive uniqueness check (friendlier than relying only on constraint)
        var existingSameName = await _db.Connection.QueryAsync<SupportedApplication>(
            "SELECT * FROM SupportedApplication WHERE Name = ? COLLATE NOCASE LIMIT 1",
            app.Name
        );

        if (existingSameName.Count > 0 && existingSameName[0].Id != app.Id)
            return new SaveSupportedApplicationResult(false, "A supported application with that name already exists.");

        try
        {
            // Requirement: update CreatedAt even when editing
            app.CreatedAt = DateTime.UtcNow;

            var updated = await _db.Connection.UpdateAsync(app);
            if (updated == 0)
            {
                if (app.Id == Guid.Empty)
                    app.Id = Guid.NewGuid();

                await _db.Connection.InsertAsync(app);
            }

            return new SaveSupportedApplicationResult(true, null, app);
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
        {
            return new SaveSupportedApplicationResult(false, "A supported application with that name already exists.");
        }
    }
}
