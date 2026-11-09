using Microsoft.EntityFrameworkCore;
using MiniHn.Infrastructure.Persistence;
using Npgsql;

namespace MiniHn.Api.Tests;

/// <summary>
/// A database of its own per test, on the shared <see cref="PostgresServer"/>,
/// dropped on dispose. It is not created here: whoever migrates first, the host
/// or the test, creates it, which is what a fresh deployment does too. A whole
/// database rather than a wiped schema, so identities start at 1 in every test.
/// </summary>
public sealed class TempPostgresDatabase : IDisposable
{
    public TempPostgresDatabase()
    {
        Name = $"minihn_test_{Guid.NewGuid():N}";
        ConnectionString = new NpgsqlConnectionStringBuilder(PostgresServer.AdminConnectionString)
        {
            Database = Name,
        }.ConnectionString;
    }

    /// <summary>The database name, unique to this test.</summary>
    public string Name { get; }

    /// <summary>Connection string pointing at <see cref="Name"/>.</summary>
    public string ConnectionString { get; }

    /// <summary>A context over this database.</summary>
    public AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Whether the server holds this database yet.</summary>
    public async Task<bool> ExistsAsync()
    {
        await using var connection = new NpgsqlConnection(PostgresServer.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        command.Parameters.AddWithValue("name", Name);
        return await command.ExecuteScalarAsync() is not null;
    }

    public void Dispose()
    {
        NpgsqlConnection.ClearAllPools();
        using var connection = new NpgsqlConnection(PostgresServer.AdminConnectionString);
        connection.Open();
        // The name is a Guid this class made, never input, so it is safe to inline.
        using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)", connection);
        command.ExecuteNonQuery();
    }
}
