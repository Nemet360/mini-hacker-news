using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniHn.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the model without booting the host. Adding a
/// migration or checking for pending model changes never opens a connection, so
/// CI can run those commands with no database and no secret.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>Used only when no connection string is supplied. Never connected to.</summary>
    public const string PlaceholderConnectionString = "Host=localhost;Database=minihn_design_time";

    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionStringFrom(Environment.GetEnvironmentVariable("ConnectionStrings__Posts")))
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>The supplied connection string, or the placeholder when there is none.</summary>
    public static string ConnectionStringFrom(string? supplied) =>
        string.IsNullOrWhiteSpace(supplied) ? PlaceholderConnectionString : supplied;
}
