using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MiniHn.Api.Tests;

public sealed class MigrationModelTests
{
    [Fact]
    public async Task Postgres_migration_applies_and_matches_model()
    {
        using var database = new TempPostgresDatabase();
        await using var context = database.NewContext();

        await context.Database.MigrateAsync();

        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();

        // The question `dotnet ef migrations has-pending-model-changes` asks, asked
        // from a test so a local run catches the drift CI would.
        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
        if (snapshot is IMutableModel mutable)
        {
            snapshot = mutable.FinalizeModel();
        }

        snapshot = context.GetService<IModelRuntimeInitializer>().Initialize(snapshot);
        var model = context.GetService<IDesignTimeModel>().Model;

        context.GetService<IMigrationsModelDiffer>()
            .HasDifferences(snapshot.GetRelationalModel(), model.GetRelationalModel())
            .Should().BeFalse("the snapshot must describe the model the code builds");
    }
}
