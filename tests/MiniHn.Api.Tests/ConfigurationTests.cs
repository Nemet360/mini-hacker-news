namespace MiniHn.Api.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void Startup_without_postgres_fails_fast_with_a_named_message()
    {
        using var factory = new ApiFactory(connectionString: "   ");

        var start = () => factory.CreateClient();

        start.Should().Throw<InvalidOperationException>()
            .WithMessage("ConnectionStrings:Posts is not configured: set it to a PostgreSQL connection string.",
                "a host with no store must fail at start, in one line that names the setting and the engine");
    }
}
