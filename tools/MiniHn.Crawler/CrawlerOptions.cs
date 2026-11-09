using System.Globalization;

namespace MiniHn.Crawler;

/// <summary>Command line of the seed tool. Parsing is pure, so it is tested.</summary>
/// <param name="Count">How many new posts to add at most.</param>
/// <param name="DatabasePath">The SQLite file to fill.</param>
/// <param name="DryRun">Print what would be written and write nothing.</param>
/// <param name="Clear">Delete every existing post before crawling.</param>
/// <param name="DelayMilliseconds">Pause between item reads, so the API is not hammered.</param>
public sealed record CrawlerOptions(
    int Count,
    string DatabasePath,
    bool DryRun,
    bool Clear,
    int DelayMilliseconds)
{
    /// <summary>Where the API keeps its store when nothing else is asked for.</summary>
    public const string DefaultDatabasePath = "src/MiniHn.Api/data/minihn.db";

    /// <summary>Printed when the arguments do not parse.</summary>
    public const string Usage = """
        MiniHn.Crawler - fills the posts store with real Hacker News stories.

          --count <n>     how many posts to add (default 25, max 500)
          --db <path>     SQLite file to fill (default src/MiniHn.Api/data/minihn.db)
          --delay <ms>    pause between API reads (default 120)
          --dry-run       print what would be added, write nothing
          --clear         delete existing posts first
          -h, --help      this text
        """;

    /// <summary>Parses the argument vector, or null when it is unusable.</summary>
    public static CrawlerOptions? Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var count = 25;
        var path = DefaultDatabasePath;
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
                case "--db" when TryValue(args, ref index, out var rawPath) && rawPath.Length > 0:
                    path = rawPath;
                    break;
                default:
                    return null;
            }
        }

        return new CrawlerOptions(count, path, dryRun, clear, delay);
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
