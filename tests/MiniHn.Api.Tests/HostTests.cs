using System.Net;

namespace MiniHn.Api.Tests;

public sealed class HostTests
{
    [Fact]
    public async Task Host_boots_and_unknown_route_returns_404()
    {
        using var database = new TempDatabase();
        using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/nope");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "an unplanned route must never appear by accident");
    }
}
