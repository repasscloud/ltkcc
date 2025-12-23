using SQLite;
using LTKCC.Models;
using LTKCC.Security;
using System.Reflection;

namespace LTKCC.Data;

public sealed class AppDb
{
    private readonly SQLiteAsyncConnection _db;
    private bool _inited;

    public AppDb()
    {
        // automatically creates the file if it doesn't exist and sets path based on OS
        var dbPath = Services.AppPaths.GetDatabasePath();

        _db = new SQLiteAsyncConnection(
            dbPath,
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache);
    }

    public SQLiteAsyncConnection Connection => _db;

    public async Task InitAsync()
    {
        if (_inited) return;
        
        // ---- SendGridSettings (singleton) ----
        await _db.CreateTableAsync<SendGridSettings>();

        var existing = await _db.Table<SendGridSettings>()
                                .Where(x => x.Id == 1)
                                .FirstOrDefaultAsync();

        if (existing is null)
        {
            var seeded = EncryptForStorage(new SendGridSettings
            {
                Id = 1,
                ApiUri = "https://api.sendgrid.com/v3/mail/send",
                FromEmail = "",
                FromName = "",
                KeyName = "",
                ApiKey = ""
            });

            await _db.InsertAsync(seeded);
        }


        // ---- Other tables ----
        await _db.CreateTableAsync<Client>();
        await _db.ExecuteAsync(
            $"CREATE UNIQUE INDEX IF NOT EXISTS IX_{TableName<Client>()}_Name_NOCASE " +
            $"ON {TableName<Client>()}(Name COLLATE NOCASE);");

        await _db.CreateTableAsync<SupportedApplication>();
        await _db.ExecuteAsync(
            $"CREATE UNIQUE INDEX IF NOT EXISTS IX_{TableName<SupportedApplication>()}_Name_NOCASE " +
            $"ON {TableName<SupportedApplication>()}(Name COLLATE NOCASE);");

        await _db.CreateTableAsync<AppEnv>();
        await _db.CreateTableAsync<HtmlTemplate>();

        await _db.CreateTableAsync<DistributionListRow>();
        await _db.CreateTableAsync<DistributionListEmailRow>();

        await _db.CreateTableAsync<WorkflowRow>();
        await _db.CreateTableAsync<WorkflowStepRow>();
        await _db.CreateTableAsync<ScheduledTaskRow>();
        await _db.CreateTableAsync<ScheduledTaskRunRow>();
        await _db.CreateTableAsync<WorkflowStepRunRow>();

        // Indexes — table names must match what sqlite-net created.
        await _db.ExecuteAsync($@"
            CREATE INDEX IF NOT EXISTS IX_{TableName<WorkflowStepRow>()}_WorkflowId_Version_Order
            ON {TableName<WorkflowStepRow>()} (WorkflowId, WorkflowVersion, StepOrder);");

        await _db.ExecuteAsync($@"
            CREATE INDEX IF NOT EXISTS IX_{TableName<ScheduledTaskRow>()}_WorkflowId_Version
            ON {TableName<ScheduledTaskRow>()} (WorkflowId, WorkflowVersion);");

        await _db.ExecuteAsync($@"
            CREATE INDEX IF NOT EXISTS IX_{TableName<ScheduledTaskRunRow>()}_TaskId_StartedUtc
            ON {TableName<ScheduledTaskRunRow>()} (ScheduledTaskId, StartedUtc);");

        await _db.ExecuteAsync($@"
            CREATE INDEX IF NOT EXISTS IX_{TableName<WorkflowStepRunRow>()}_RunId_Order
            ON {TableName<WorkflowStepRunRow>()} (ScheduledTaskRunId, StepOrder);");

        _inited = true;
    }

    // Save (encrypt on write)
    public Task SaveSendGridSettingsAsync(SendGridSettings settings)
    {
        if (settings is null) throw new ArgumentNullException(nameof(settings));

        settings.Id = 1;
        var toStore = EncryptForStorage(settings);
        return _db.InsertOrReplaceAsync(toStore);
    }

    // Read (decrypt on read)
    public async Task<SendGridSettings> GetSendGridSettingsAsync()
    {
        var row = await _db.Table<SendGridSettings>()
                           .Where(x => x.Id == 1)
                           .FirstOrDefaultAsync();

        return row is null
            ? new SendGridSettings { Id = 1 }
            : DecryptFromStorage(row);
    }

    private static SendGridSettings EncryptForStorage(SendGridSettings s) => new()
    {
        Id        = 1,
        ApiUri    = s.ApiUri,
        FromEmail = s.FromEmail,
        FromName  = s.FromName,
        KeyName   = EncryptIfNeeded(s.KeyName),
        ApiKey    = EncryptIfNeeded(s.ApiKey),
    };

    private static SendGridSettings DecryptFromStorage(SendGridSettings s) => new()
    {
        Id        = 1,
        ApiUri    = s.ApiUri,
        FromEmail = s.FromEmail,
        FromName  = s.FromName,

        // Do NOT call Decrypt() unless it looks encrypted
        KeyName   = DecryptIfNeeded(s.KeyName),
        ApiKey    = DecryptIfNeeded(s.ApiKey),
    };

    private static bool IsEncrypted(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        return value.StartsWith("enc:v2:", StringComparison.Ordinal)
            || value.StartsWith("enc:v1:", StringComparison.Ordinal);
    }

    private static string DecryptIfNeeded(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return IsEncrypted(value) ? EncryptionTool.Decrypt(value) : value;
    }

    private static string EncryptIfNeeded(string? value)
    {
        value ??= string.Empty;

        return (value.StartsWith("enc:v2:", StringComparison.Ordinal) ||
                value.StartsWith("enc:v1:", StringComparison.Ordinal))
            ? value
            : EncryptionTool.Encrypt(value);
    }

    // Returns the real sqlite table name, respecting [Table("Name")] when present.
    private static string TableName<T>()
    {
        var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
        return string.IsNullOrWhiteSpace(tableAttr?.Name)
            ? typeof(T).Name
            : tableAttr!.Name;
    }
}
