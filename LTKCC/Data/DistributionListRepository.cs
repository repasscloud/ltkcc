// File: Data/DistributionListRepository.cs
using LTKCC.Models;

namespace LTKCC.Data;

public sealed class DistributionListRepository
{
    private readonly AppDb _db;

    public DistributionListRepository(AppDb db) => _db = db;

    public async Task<List<DistributionListRow>> GetAllAsync()
    {
        await _db.InitAsync();
        return await _db.Connection.Table<DistributionListRow>()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<(DistributionListRow? list, List<DistributionListEmailRow> emails)> GetByIdAsync(int id)
    {
        await _db.InitAsync();

        var list = await _db.Connection.Table<DistributionListRow>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();

        if (list is null)
            return (null, new List<DistributionListEmailRow>());

        var emails = await _db.Connection.Table<DistributionListEmailRow>()
            .Where(x => x.DistributionListId == id)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Email)
            .ToListAsync();

        return (list, emails);
    }

    public async Task<int> UpsertAsync(DistributionListRow list, IEnumerable<DistributionListEmailRow> emails)
    {
        await _db.InitAsync();

        list.UpdatedUtcTicks = DateTime.UtcNow.Ticks;

        await _db.Connection.RunInTransactionAsync(tran =>
        {
            // Insert/update list
            if (list.Id <= 0)
            {
                tran.Insert(list);
            }
            else
            {
                tran.Update(list);
                // Replace email rows for simplicity (matches your “edit rows then Save” flow)
                tran.Execute("DELETE FROM DistributionListEmail WHERE DistributionListId = ?", list.Id);
            }

            // After insert, list.Id is set by sqlite-net in the same object instance.
            foreach (var e in emails)
            {
                var row = new DistributionListEmailRow
                {
                    DistributionListId = list.Id,
                    Email = (e.Email ?? "").Trim(),
                    DisplayName = (e.DisplayName ?? "").Trim()
                };

                if (string.IsNullOrWhiteSpace(row.Email))
                    continue;

                tran.Insert(row);
            }
        });

        return list.Id;
    }

    public async Task DeleteAsync(int id)
    {
        await _db.InitAsync();

        await _db.Connection.RunInTransactionAsync(tran =>
        {
            tran.Execute("DELETE FROM DistributionListEmail WHERE DistributionListId = ?", id);
            tran.Execute("DELETE FROM DistributionList WHERE Id = ?", id);
        });
    }
}
