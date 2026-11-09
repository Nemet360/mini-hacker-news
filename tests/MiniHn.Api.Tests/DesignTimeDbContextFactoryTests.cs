using MiniHn.Infrastructure.Persistence;

namespace MiniHn.Api.Tests;

public sealed class DesignTimeDbContextFactoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void No_connection_string_falls_back_to_a_placeholder_the_tools_never_open(string? supplied)
    {
        DesignTimeDbContextFactory.ConnectionStringFrom(supplied)
            .Should().Be(DesignTimeDbContextFactory.PlaceholderConnectionString);
    }

    [Fact]
    public void A_supplied_connection_string_is_used_as_is()
    {
        DesignTimeDbContextFactory.ConnectionStringFrom("Host=db;Database=x").Should().Be("Host=db;Database=x");
    }

    [Fact]
    public void The_design_time_context_uses_the_postgresql_provider()
    {
        using var context = new DesignTimeDbContextFactory().CreateDbContext([]);

        context.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }
}
