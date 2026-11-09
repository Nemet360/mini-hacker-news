using Microsoft.EntityFrameworkCore;
using MiniHn.Infrastructure.Persistence;

namespace MiniHn.Api.Tests;

/// <summary>
/// A unique SQLite FILE per test, deleted on dispose. Not :memory: - the requirement
/// under test is persistence, and only a file can be reopened by a second host.
/// </summary>
public sealed class TempDatabase : IDisposable
{
    public TempDatabase()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"minihn-{Guid.NewGuid():N}.db");
    }

    /// <summary>Absolute path of the database file.</summary>
    public string Path { get; }

    /// <summary>Connection string pointing at <see cref="Path"/>.</summary>
    public string ConnectionString => $"Data Source={Path}";

    /// <summary>A context over this file.</summary>
    public AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
        {
            var file = Path + suffix;
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    // A file the OS still holds is temp-directory litter, not a test failure.
                }
            }
        }
    }
}
