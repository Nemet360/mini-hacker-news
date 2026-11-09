namespace MiniHn.Crawler.Tests;

public sealed class CrawlerOptionsTests
{
    [Fact]
    public void No_arguments_give_the_api_store_and_one_hundred_posts()
    {
        var options = CrawlerOptions.Parse([]);

        options.Should().NotBeNull();
        options!.Count.Should().Be(100);
        options.ConnectionString.Should().BeNull("the store comes from the same variable the API reads");
        options.DryRun.Should().BeFalse();
        options.Clear.Should().BeFalse();
        options.DelayMilliseconds.Should().Be(120);
    }

    [Fact]
    public void Every_flag_is_read()
    {
        var options = CrawlerOptions.Parse(
            ["--count", "7", "--connection", "Host=db;Database=x", "--delay", "0", "--dry-run", "--clear"]);

        options.Should().NotBeNull();
        options!.Count.Should().Be(7);
        options.ConnectionString.Should().Be("Host=db;Database=x");
        options.DelayMilliseconds.Should().Be(0);
        options.DryRun.Should().BeTrue();
        options.Clear.Should().BeTrue();
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("--what")]
    [InlineData("--count")]          // a flag with no value
    [InlineData("--count,0")]
    [InlineData("--count,501")]
    [InlineData("--count,many")]
    [InlineData("--delay,-1")]
    [InlineData("--delay,10001")]
    [InlineData("--connection,")]
    [InlineData("--connection, ")]
    [InlineData("--db,x.db")]         // the SQLite flag is gone, not silently ignored
    public void An_unusable_command_line_is_refused_rather_than_guessed(string arguments)
    {
        var argv = arguments.Split(',');

        CrawlerOptions.Parse(argv).Should().BeNull(
            "a seed tool that silently crawls 100 stories because the count was a typo is worse than one that prints the usage");
    }
}
