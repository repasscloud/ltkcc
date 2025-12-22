using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using SQLite;

namespace LTKCC.Data;

public sealed class AppDb
{
    private readonly SQLiteAsyncConnection _db;

    public AppDb()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "ltkcc.db3");

        _db = new SQLiteAsyncConnection(
            dbPath,
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache);
    }

    // public Task InitAsync() => _db.CreateTableAsync<Setting>();

    // public Task<int> UpsertSettingAsync(string key, string value)
    //     => _db.InsertOrReplaceAsync(new Setting { Key = key, Value = value });

    // public Task<Setting?> GetSettingAsync(string key)
    //     => _db.Table<Setting>().Where(x => x.Key == key).FirstOrDefaultAsync();

    // public Task<int> DeleteSettingAsync(string key)
    //     => _db.Table<Setting>().DeleteAsync(x => x.Key == key);
}
