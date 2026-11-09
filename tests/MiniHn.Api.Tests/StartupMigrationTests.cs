using Microsoft.EntityFrameworkCore;

namespace MiniHn.Api.Tests;

public sealed class StartupMigrationTests
{
    [Fact]
    public async Task Fresh_database_file_is_created_and_migrated_on_host_start()
    {
        using var database = new TempDatabase();
        File.Exists(database.Path).Should().BeFalse("the file must not exist before the host runs");

        using (var factory = new ApiFactory(database.ConnectionString))
        {
            using var client = factory.CreateClient();   // forces the host to start
            File.Exists(database.Path).Should().BeTrue("the host creates the store it needs");
        }

        await using var context = database.NewContext();
        var applied = await context.Database.GetAppliedMigrationsAsync();
        applied.Should().ContainSingle().Which.Should().EndWith("InitialCreate");
    }
}
