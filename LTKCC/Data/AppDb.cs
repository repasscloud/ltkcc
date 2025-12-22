using System.IO;
using System.Threading.Tasks;
using LTKCC.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace LTKCC.Data;

public sealed class AppDb
{
    private readonly SQLiteAsyncConnection _db;
    public SQLiteAsyncConnection Db => _db;

    public AppDb()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "ltkcc.db3");

        _db = new SQLiteAsyncConnection(
            dbPath,
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache);
    }

    public SQLiteAsyncConnection Connection => _db;

    // Initialize database tables
    public async Task InitAsync()
    {
        await _db.CreateTableAsync<Client>();
        // Enforce uniqueness case-insensitively too (optional but recommended).
        // Default table name for sqlite-net is the class name: "Client".
        await _db.ExecuteAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_Client_Name_NOCASE ON Client(Name COLLATE NOCASE);"
        );

        await _db.CreateTableAsync<SupportedApplication>();
        await _db.ExecuteAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_SupportedApplication_Name_NOCASE ON SupportedApplication(Name COLLATE NOCASE);"
        );

        await _db.CreateTableAsync<AppEnv>();
        
        await _db.CreateTableAsync<HtmlTemplate>();

        await _db.CreateTableAsync<DistributionListRow>();
        await _db.CreateTableAsync<DistributionListEmailRow>();

        await _db.CreateTableAsync<WorkflowRow>();
        await _db.CreateTableAsync<WorkflowStepRow>();
        await _db.CreateTableAsync<ScheduledTaskRow>();
        await _db.CreateTableAsync<ScheduledTaskRunRow>();
        await _db.CreateTableAsync<WorkflowStepRunRow>();

        // Useful indexes (sqlite-net attribute indexes are fine, but these help enforce patterns).
        await _db.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS IX_WorkflowSteps_WorkflowId_Version_Order
            ON WorkflowSteps (WorkflowId, WorkflowVersion, StepOrder);");

        await _db.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS IX_ScheduledTasks_WorkflowId_Version
            ON ScheduledTasks (WorkflowId, WorkflowVersion);");

        await _db.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS IX_ScheduledTaskRuns_TaskId_StartedUtc
            ON ScheduledTaskRuns (ScheduledTaskId, StartedUtc);");

        await _db.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS IX_WorkflowStepRuns_RunId_Order
            ON WorkflowStepRuns (ScheduledTaskRunId, StepOrder);");
    }

    // public Task InitAsync() => _db.CreateTableAsync<Setting>();

    // public Task<int> UpsertSettingAsync(string key, string value)
    //     => _db.InsertOrReplaceAsync(new Setting { Key = key, Value = value });

    // public Task<Setting?> GetSettingAsync(string key)
    //     => _db.Table<Setting>().Where(x => x.Key == key).FirstOrDefaultAsync();

    // public Task<int> DeleteSettingAsync(string key)
    //     => _db.Table<Setting>().DeleteAsync(x => x.Key == key);
}
