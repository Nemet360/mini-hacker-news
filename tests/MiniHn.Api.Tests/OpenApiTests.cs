using System.Net;

namespace MiniHn.Api.Tests;

public sealed class OpenApiTests
{
    [Fact]
    public async Task Development_serves_the_openapi_document_describing_get_posts()
    {
        using var database = new TempPostgresDatabase();
        using var factory = new ApiFactory(database.ConnectionString, "Development");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("\"/posts\"");
    }

    [Fact]
    public async Task Production_serves_no_openapi_document_and_no_swagger_ui()
    {
        using var database = new TempPostgresDatabase();
        using var factory = new ApiFactory(database.ConnectionString, "Production");
        using var client = factory.CreateClient();

        (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode.Should().Be(HttpStatusCode.NotFound,
            "production serves the API only");
        (await client.GetAsync("/swagger")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
