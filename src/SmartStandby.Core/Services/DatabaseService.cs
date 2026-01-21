using SQLite;
using System.Linq;
using SmartStandby.Core.Models;
using System.IO;

namespace SmartStandby.Core.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _database;
    private const string DbFileName = "SmartStandby.db3";

    public async Task InitializeAsync()
    {
        if (_database != null)
            return;

        // Save DB in local application data folder
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartStandby");
        
        if (!Directory.Exists(dbPath))
        {
            Directory.CreateDirectory(dbPath);
        }

        var fullPath = Path.Combine(dbPath, DbFileName);
        
        _database = new SQLiteAsyncConnection(fullPath);

        // Create tables if they don't exist
        await _database.CreateTableAsync<SleepSession>();
        await _database.CreateTableAsync<AppConfig>();
        await _database.CreateTableAsync<ProcessWhitelist>();
    }

    public SQLiteAsyncConnection GetConnection() => _database;

    // --- SleepSession ---
    public async Task AddSessionAsync(SleepSession session)
    {
        await InitializeAsync();
        await _database.InsertAsync(session);
    }

    public async Task UpdateSessionAsync(SleepSession session)
    {
        await InitializeAsync();
        await _database.UpdateAsync(session);
    }

    public async Task<List<SleepSession>> GetRecentSessionsAsync(int limit = 20)
    {
        await InitializeAsync();
        return await _database.Table<SleepSession>()
                            .OrderByDescending(s => s.SleepTime)
                            .Take(limit)
                            .ToListAsync();
    }

    public async Task<List<SleepSession>> GetSessionsAfterAsync(DateTime date)
    {
        await InitializeAsync();
        return await _database.Table<SleepSession>()
                            .Where(s => s.SleepTime >= date)
                            .OrderBy(s => s.SleepTime)
                            .ToListAsync();
    }

    // --- AppConfig ---
    public async Task<string?> GetConfigAsync(string key)
    {
        await InitializeAsync();
        var config = await _database.Table<AppConfig>().Where(c => c.Key == key).FirstOrDefaultAsync();
        return config?.Value;
    }

    public async Task SetConfigAsync(string key, string value)
    {
        await InitializeAsync();
        var config = new AppConfig { Key = key, Value = value };
        await _database.InsertOrReplaceAsync(config);
    }

    public async Task<bool> GetConfigBoolAsync(string key, bool defaultValue)
    {
        var val = await GetConfigAsync(key);
        if (bool.TryParse(val, out var result)) return result;
        return defaultValue;
    }

    public async Task SetConfigBoolAsync(string key, bool value)
    {
        await SetConfigAsync(key, value.ToString());
    }

    // --- Whitelist ---
    public async Task<List<string>> GetWhitelistAsync()
    {
        await InitializeAsync();
        var list = await _database.Table<ProcessWhitelist>().ToListAsync();
        return list.Select(x => x.ProcessName).ToList();
    }

    public async Task AddWhitelistAsync(string processName)
    {
        await InitializeAsync();
        try
        {
            await _database.InsertAsync(new ProcessWhitelist { ProcessName = processName });
        }
        catch (SQLiteException) { /* Ensure unique constraint is handled gracefully */ }
    }

    public async Task RemoveWhitelistAsync(string processName)
    {
        await InitializeAsync();
        var item = await _database.Table<ProcessWhitelist>().Where(x => x.ProcessName == processName).FirstOrDefaultAsync();
        if (item != null)
        {
            await _database.DeleteAsync(item);
        }
    }
}
