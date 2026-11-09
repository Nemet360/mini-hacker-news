using Microsoft.AspNetCore.Mvc.Testing;

namespace MiniHn.Api.Tests;

public sealed class HostTests
{
    [Fact]
    public async Task Host_boots_and_unknown_route_returns_404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/nope");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
