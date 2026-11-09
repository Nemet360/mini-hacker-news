using Microsoft.EntityFrameworkCore;
using MiniHn.Domain;

namespace MiniHn.Api.Tests;

public sealed class PostConfigurationTests
{
    [Fact]
    public async Task Context_round_trips_a_post_through_a_temp_sqlite_file()
    {
        using var database = new TempDatabase();

        await using (var context = database.NewContext())
        {
            await context.Database.MigrateAsync();
            context.Posts.Add(new Post { Content = "first post" });
            await context.SaveChangesAsync();
        }

        await using (var context = database.NewContext())
        {
            var stored = await context.Posts.AsNoTracking().SingleAsync();
            stored.Id.Should().BeGreaterThan(0, "the store assigns the identity");
            stored.Content.Should().Be("first post");
        }
    }

    [Fact]
    public async Task Migration_applies_to_an_empty_database_and_creates_posts_table()
    {
        using var database = new TempDatabase();
        await using var context = database.NewContext();

        await context.Database.MigrateAsync();

        var tables = await context.Database
            .SqlQuery<string>($"SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToListAsync();

        tables.Should().Contain("posts");

        var schema = await context.Database
            .SqlQuery<string>($"SELECT sql AS Value FROM sqlite_master WHERE type = 'table' AND name = 'posts'")
            .SingleAsync();

        schema.Should().Contain("AUTOINCREMENT");
        schema.Should().Contain("ck_posts_content_length");
    }

    [Fact]
    public async Task Content_longer_than_the_bound_is_refused_by_the_store()
    {
        using var database = new TempDatabase();
        await using var context = database.NewContext();
        await context.Database.MigrateAsync();

        context.Posts.Add(new Post { Content = new string('x', 10_001) });

        var save = async () => await context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>(
            "a column with no bound is a column that accepts a 2 GB row");
    }
}
