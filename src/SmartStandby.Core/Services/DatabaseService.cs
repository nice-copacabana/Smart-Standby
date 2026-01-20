using SQLite;
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
    }

    public SQLiteAsyncConnection GetConnection()
    {
        return _database;
    }

    public async Task AddSessionAsync(SleepSession session)
    {
        await InitializeAsync();
        await _database.InsertAsync(session);
    }

    public async Task<List<SleepSession>> GetRecentSessionsAsync(int limit = 20)
    {
        await InitializeAsync();
        return await _database.Table<SleepSession>()
                            .OrderByDescending(s => s.SleepTime)
                            .Take(limit)
                            .ToListAsync();
    }
}
