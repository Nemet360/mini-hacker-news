using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MiniHn.Api.Tests;

/// <summary>
/// Hosts the real application over a caller supplied PostgreSQL database. Nothing is
/// stubbed: the same provider, the same migrations and the same endpoints the
/// deployed host runs.
/// </summary>
public sealed class ApiFactory(string connectionString, string environment = "Development")
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Posts", connectionString);
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Posts"] = connectionString,
            }));
    }
}
