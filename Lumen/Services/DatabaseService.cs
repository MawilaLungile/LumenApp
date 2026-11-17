using SQLite;

namespace Lumen.Services;

/// <summary>
/// Async SQLite repository. The connection is created lazily and thread-safely
/// on first use; demo data is seeded on the very first launch.
/// </summary>
public class DatabaseService
{
    private const string DbFileName = "lumen.db3";
    private const string SeededKey = "db_seeded";

    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private SQLiteAsyncConnection? _db;

    public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DbFileName);

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;

        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return _db;

            var db = new SQLiteAsyncConnection(DatabasePath, Flags);
            await db.CreateTableAsync<TransactionEntry>();
            await db.CreateTableAsync<Budget>();
            await db.CreateTableAsync<SavingsGoal>();

            if (!Preferences.Default.Get(SeededKey, false))
            {
                await SeedData.SeedAsync(db);
                Preferences.Default.Set(SeededKey, true);
            }

            _db = db;
            return _db;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ------------------------------------------------------------------ Transactions

    public async Task<List<TransactionEntry>> GetTransactionsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<TransactionEntry>().OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task<TransactionEntry?> GetTransactionAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.FindAsync<TransactionEntry>(id);
    }

    public async Task SaveTransactionAsync(TransactionEntry entry)
    {
        var db = await GetConnectionAsync();
        if (entry.Id == 0) await db.InsertAsync(entry);
        else await db.UpdateAsync(entry);
    }

    public async Task DeleteTransactionAsync(TransactionEntry entry)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(entry);
    }

    // ------------------------------------------------------------------ Budgets

    public async Task<List<Budget>> GetBudgetsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Budget>().ToListAsync();
    }

    public async Task SaveBudgetAsync(Budget budget)
    {
        var db = await GetConnectionAsync();
        if (budget.Id == 0) await db.InsertAsync(budget);
        else await db.UpdateAsync(budget);
    }

    public async Task DeleteBudgetAsync(Budget budget)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(budget);
    }

    // ------------------------------------------------------------------ Goals

    public async Task<List<SavingsGoal>> GetGoalsAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<SavingsGoal>().OrderBy(g => g.Deadline).ToListAsync();
    }

    public async Task SaveGoalAsync(SavingsGoal goal)
    {
        var db = await GetConnectionAsync();
        if (goal.Id == 0) await db.InsertAsync(goal);
        else await db.UpdateAsync(goal);
    }

    public async Task DeleteGoalAsync(SavingsGoal goal)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAsync(goal);
    }

    // ------------------------------------------------------------------ Maintenance

    /// <summary>Wipes all data. When <paramref name="reseed"/> is true, demo data is regenerated.</summary>
    public async Task ResetAsync(bool reseed)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAllAsync<TransactionEntry>();
        await db.DeleteAllAsync<Budget>();
        await db.DeleteAllAsync<SavingsGoal>();

        if (reseed) await SeedData.SeedAsync(db);
    }
}
