// File: Data/WorkflowStepDefinitionRepository.cs
using System.Text.Json;
using SQLite;
using LTKCC.Models;

namespace LTKCC.Data;

public sealed class WorkflowStepDefinitionRepository
{
    private readonly SQLiteAsyncConnection _db;

    public WorkflowStepDefinitionRepository(AppDb appDb)
    {
        _db = appDb.Connection;
    }

    public Task<List<WorkflowStepDefinitionRow>> GetAllAsync()
        => _db.Table<WorkflowStepDefinitionRow>()
              .OrderByDescending(x => x.CreatedUtc)
              .ToListAsync();

    public async Task<WorkflowStepDefinitionRow?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        // sqlite-net's FirstOrDefaultAsync() may return null at runtime even if its signature is non-nullable.
        var row = await _db.Table<WorkflowStepDefinitionRow>()
                           .Where(x => x.Id == id)
                           .FirstOrDefaultAsync();

        return row;
    }

    public async Task<bool> NameExistsAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var count = await _db.Table<WorkflowStepDefinitionRow>()
                             .Where(x => x.Name == name)
                             .CountAsync();

        return count > 0;
    }

    public Task<int> InsertAsync(WorkflowStepDefinitionRow row)
        => _db.InsertAsync(row);

    public static string ToKeysJson(IEnumerable<string> keys)
        => JsonSerializer.Serialize(keys);

    public static IReadOnlyList<string> FromKeysJson(string json)
        => JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? new List<string>();

    public Task<int> DeleteByIdAsync(string id)
        => _db.Table<WorkflowStepDefinitionRow>()
          .Where(x => x.Id == id)
          .DeleteAsync();

    public Task<int> DeleteAsync(WorkflowStepDefinitionRow row)
        => _db.DeleteAsync(row);
}
