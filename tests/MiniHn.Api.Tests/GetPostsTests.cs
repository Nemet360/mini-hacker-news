using System.Net;

namespace MiniHn.Api.Tests;

public sealed class GetPostsTests
{
    [Fact]
    public async Task Get_posts_on_empty_database_returns_200_and_empty_array()
    {
        using var database = new TempDatabase();
        using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/posts");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        body.Should().Be("[]");
    }
}
