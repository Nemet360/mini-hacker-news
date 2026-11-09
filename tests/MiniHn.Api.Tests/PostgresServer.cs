using Testcontainers.PostgreSql;

namespace MiniHn.Api.Tests;

/// <summary>
/// The one PostgreSQL server the whole test run shares. CI hands one in through
/// <c>TEST_PG_CONNECTION</c> (a service container); a local run with nothing set
/// starts a throwaway container once, and Testcontainers reaps it on exit.
/// Tests never share a database on it: see <see cref="TempPostgresDatabase"/>.
/// </summary>
public static class PostgresServer
{
    /// <summary>The variable CI sets. It points at a maintenance database, normally <c>postgres</c>.</summary>
    public const string Variable = "TEST_PG_CONNECTION";

    /// <summary>The image a local run starts: the same major as compose and CI.</summary>
    public const string Image = "postgres:17-alpine";

    private static readonly Lazy<string> _server = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>An administrative connection string for the shared server.</summary>
    public static string AdminConnectionString => _server.Value;

    private static string Start()
    {
        var supplied = Environment.GetEnvironmentVariable(Variable);
        if (!string.IsNullOrWhiteSpace(supplied))
        {
            return supplied;
        }

        var container = new PostgreSqlBuilder(Image).Build();
        container.StartAsync().GetAwaiter().GetResult();
        return container.GetConnectionString();
    }
}
