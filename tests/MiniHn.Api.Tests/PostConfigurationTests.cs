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
            await context.Database.EnsureCreatedAsync();
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
}
