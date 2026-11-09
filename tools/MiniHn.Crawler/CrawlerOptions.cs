using System.Globalization;

namespace MiniHn.Crawler;

/// <summary>Command line of the seed tool. Parsing is pure, so it is tested.</summary>
/// <param name="Count">How many new posts to add at most.</param>
/// <param name="ConnectionString">The PostgreSQL store to fill, or null to read <see cref="ConnectionStringVariable"/>.</param>
/// <param name="DryRun">Print what would be written and write nothing.</param>
/// <param name="Clear">Delete every existing post before crawling.</param>
/// <param name="DelayMilliseconds">Pause between item reads, so the API is not hammered.</param>
public sealed record CrawlerOptions(
    int Count,
    string? ConnectionString,
    bool DryRun,
    bool Clear,
    int DelayMilliseconds)
{
    /// <summary>How many posts a run adds when --count is not given: enough to make a demo feed look like one.</summary>
    public const int DefaultCount = 100;

    /// <summary>The environment variable the API reads its store from; the crawler fills the same store.</summary>
    public const string ConnectionStringVariable = "ConnectionStrings__Posts";

    /// <summary>Printed when the arguments do not parse.</summary>
    public const string Usage = """
        MiniHn.Crawler - fills the posts store with real Hacker News stories.

          --count <n>          how many posts to add (default 100, max 500)
          --connection <cs>    PostgreSQL connection string (default: $ConnectionStrings__Posts)
          --delay <ms>         pause between API reads (default 120)
          --dry-run            print what would be added, write nothing
          --clear              delete existing posts first
          -h, --help           this text
        """;

    /// <summary>Parses the argument vector, or null when it is unusable.</summary>
    public static CrawlerOptions? Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var count = DefaultCount;
        string? connection = null;
        var delay = 120;
        var dryRun = false;
        var clear = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--clear":
                    clear = true;
                    break;
                case "-h" or "--help":
                    return null;
                case "--count" when TryValue(args, ref index, out var rawCount)
                                    && int.TryParse(rawCount, CultureInfo.InvariantCulture, out var parsedCount)
                                    && parsedCount is > 0 and <= 500:
                    count = parsedCount;
                    break;
                case "--delay" when TryValue(args, ref index, out var rawDelay)
                                    && int.TryParse(rawDelay, CultureInfo.InvariantCulture, out var parsedDelay)
                                    && parsedDelay is >= 0 and <= 10_000:
                    delay = parsedDelay;
                    break;
                case "--connection" when TryValue(args, ref index, out var rawConnection)
                                         && !string.IsNullOrWhiteSpace(rawConnection):
                    connection = rawConnection;
                    break;
                default:
                    return null;
            }
        }

        return new CrawlerOptions(count, connection, dryRun, clear, delay);
    }

    private static bool TryValue(string[] args, ref int index, out string value)
    {
        if (index + 1 < args.Length)
        {
            value = args[index + 1];
            index++;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
