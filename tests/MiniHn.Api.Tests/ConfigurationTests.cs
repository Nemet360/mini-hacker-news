namespace MiniHn.Api.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void Host_refuses_to_start_without_a_connection_string()
    {
        using var factory = new ApiFactory(connectionString: "   ");

        var start = () => factory.CreateClient();

        start.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:Posts*",
                "a host with no store must fail at start, not on the first request");
    }
}
