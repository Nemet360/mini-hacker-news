using Microsoft.EntityFrameworkCore;
using MiniHn.Domain;

namespace MiniHn.Api.Tests;

public sealed class PostConfigurationTests
{
    [Fact]
    public async Task Context_round_trips_a_post_through_postgresql()
    {
        using var database = new TempPostgresDatabase();

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
    public async Task Migration_creates_posts_with_bigint_identity_text_content_and_the_length_check()
    {
        using var database = new TempPostgresDatabase();
        await using var context = database.NewContext();

        await context.Database.MigrateAsync();

        var columns = await context.Database
            .SqlQuery<ColumnRow>($"""
                SELECT column_name AS "Name", data_type AS "Type", is_nullable AS "Nullable",
                       is_identity AS "Identity", COALESCE(identity_generation, '') AS "Generation"
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'posts'
                ORDER BY ordinal_position
                """)
            .ToListAsync();

        columns.Should().Equal(
            new ColumnRow("id", "bigint", "NO", "YES", "BY DEFAULT"),
            new ColumnRow("content", "text", "NO", "NO", ""));

        var check = await context.Database
            .SqlQuery<string>($"""
                SELECT pg_get_constraintdef(oid) AS "Value"
                FROM pg_constraint
                WHERE conname = 'ck_posts_content_length'
                """)
            .SingleAsync();

        check.Should().Contain("char_length(content)").And.Contain("10000");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_001)]
    public async Task Content_outside_the_bound_is_refused_by_the_store(int length)
    {
        using var database = new TempPostgresDatabase();
        await using var context = database.NewContext();
        await context.Database.MigrateAsync();

        context.Posts.Add(new Post { Content = new string('x', length) });

        var save = async () => await context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>(
            "a column with no bound accepts a 2 GB row, and an empty post is not a post");
    }

    [Fact]
    public async Task Content_at_the_bound_counts_characters_not_bytes()
    {
        using var database = new TempPostgresDatabase();
        await using var context = database.NewContext();
        await context.Database.MigrateAsync();

        // 10,000 two-byte characters: 20,000 bytes but 10,000 characters, inside the bound.
        context.Posts.Add(new Post { Content = new string('é', 10_000) });

        var save = async () => await context.SaveChangesAsync();

        await save.Should().NotThrowAsync("char_length counts characters, so a non-ASCII post gets the same room");
    }

    private sealed record ColumnRow(string Name, string Type, string Nullable, string Identity, string Generation);
}
