using System.Net;
using Microsoft.EntityFrameworkCore;
using MiniHn.Domain;

namespace MiniHn.Api.Tests;

public sealed class GetPostsTests
{
    [Fact]
    public async Task Get_posts_on_empty_database_returns_200_and_empty_array()
    {
        using var database = new TempPostgresDatabase();
        using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/posts");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        body.Should().Be("[]");
    }

    [Fact]
    public async Task Get_posts_returns_every_stored_post_with_id_and_content()
    {
        using var database = new TempPostgresDatabase();
        using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        await Seed(database, "first post", "second post");

        var body = await client.GetStringAsync("/posts");

        body.Should().Be("""[{"id":1,"content":"first post"},{"id":2,"content":"second post"}]""");
    }

    [Fact]
    public async Task Posts_survive_a_host_restart()
    {
        using var database = new TempPostgresDatabase();

        using (var first = new ApiFactory(database.ConnectionString))
        {
            using var client = first.CreateClient();
            await Seed(database, "written before the restart");
            (await client.GetStringAsync("/posts")).Should().Contain("written before the restart");
        }

        using var second = new ApiFactory(database.ConnectionString);
        using var clientAfterRestart = second.CreateClient();

        var body = await clientAfterRestart.GetStringAsync("/posts");

        body.Should().Be("""[{"id":1,"content":"written before the restart"}]""",
            "the store outlives the process, so a second host sees what the first one wrote");
    }

    [Fact]
    public async Task Get_posts_does_not_track_entities()
    {
        using var database = new TempPostgresDatabase();
        using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();
        await Seed(database, "only post");

        await client.GetStringAsync("/posts");

        await using var context = database.NewContext();
        context.ChangeTracker.Entries().Should().BeEmpty("a read path has nothing to track");
    }

    private static async Task Seed(TempPostgresDatabase database, params string[] contents)
    {
        await using var context = database.NewContext();
        await context.Database.MigrateAsync();
        foreach (var content in contents)
        {
            context.Posts.Add(new Post { Content = content });
        }

        await context.SaveChangesAsync();
    }
}
