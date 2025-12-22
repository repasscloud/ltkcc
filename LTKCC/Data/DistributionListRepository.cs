using SQLite;
using LTKCC.Models;
using LTKCC.Validation;

namespace LTKCC.Data;

public sealed class DistributionListRepository
{
    private readonly SQLiteAsyncConnection _db;

    public DistributionListRepository(AppDb appDb) => _db = appDb.Db;

    public async Task InitAsync()
    {
        await _db.CreateTableAsync<DistributionListRow>();
        await _db.CreateTableAsync<DistributionListEmailRow>();

        await _db.ExecuteAsync(@"
CREATE UNIQUE INDEX IF NOT EXISTS UX_DLEmails_ListId_Email
ON DistributionListEmails (DistributionListId, Email);");
    }

    public Task<List<DistributionListRow>> ListAsync()
        => _db.Table<DistributionListRow>()
              .OrderBy(x => x.Name)
              .ToListAsync();

    public async Task<DistributionListRow?> GetAsync(Guid id)
    {
        var dl = await _db.FindAsync<DistributionListRow>(id);
        if (dl is null) return null;

        var emails = await _db.Table<DistributionListEmailRow>()
            .Where(x => x.DistributionListId == id)
            .OrderBy(x => x.Email)
            .ToListAsync();

        dl.Emails = emails.Select(x => x.Email).ToList();
        return dl;
    }

    public async Task UpsertAsync(DistributionListRow dl)
    {
        if (dl.Id == Guid.Empty) dl.Id = Guid.NewGuid();

        var normalized = dl.Emails
            .Select(EmailValidator.NormalizeOrThrow)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        await _db.RunInTransactionAsync(tran =>
        {
            tran.InsertOrReplace(dl);

            tran.Execute("DELETE FROM DistributionListEmails WHERE DistributionListId = ?", dl.Id);

            foreach (var email in normalized)
            {
                tran.Insert(new DistributionListEmailRow
                {
                    Id = Guid.NewGuid(),
                    DistributionListId = dl.Id,
                    Email = email
                });
            }
        });

        dl.Emails = normalized;
    }
}
