using SQLite;
using LTKCC.Data;
using LTKCC.Models;

namespace LTKCC.Services;

public sealed class ClientService : IClientService
{
    private readonly AppDb _db;

    public ClientService(AppDb db) => _db = db;

    public async Task<IReadOnlyList<Client>> GetAllAsync()
    {
        await _db.InitAsync();
        return await _db.Connection
            .Table<Client>()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<Client?> GetByIdAsync(Guid id)
    {
        await _db.InitAsync();
        return await _db.Connection
            .Table<Client>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<SaveClientResult> UpsertAsync(Client client)
    {
        await _db.InitAsync();

        var name = (client.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return new SaveClientResult(false, "Name is required.");

        // normalize
        client.Name = name;
        client.Description = string.IsNullOrWhiteSpace(client.Description) ? null : client.Description.Trim();

        // Case-insensitive uniqueness check (friendlier error than throwing).
        // Exclude same Id when updating.
        var existingSameName = await _db.Connection.QueryAsync<Client>(
            "SELECT * FROM Client WHERE Name = ? COLLATE NOCASE LIMIT 1",
            client.Name
        );

        if (existingSameName.Count > 0 && existingSameName[0].Id != client.Id)
            return new SaveClientResult(false, "A client with that name already exists.");

        try
        {
            // Requirement: update CreatedAt even on edit.
            client.CreatedAt = DateTime.UtcNow;

            // sqlite-net UpdateAsync returns 0 if row doesn't exist.
            var updated = await _db.Connection.UpdateAsync(client);
            if (updated == 0)
            {
                // If caller provided empty Guid somehow, make sure it has one.
                if (client.Id == Guid.Empty)
                    client.Id = Guid.NewGuid();

                await _db.Connection.InsertAsync(client);
            }

            return new SaveClientResult(true, null, client);
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
        {
            return new SaveClientResult(false, "A client with that name already exists.");
        }
    }
}
