using Microsoft.EntityFrameworkCore;
using MiniHn.Crawler;
using MiniHn.Domain;
using MiniHn.Infrastructure.Persistence;

// A development tool, deliberately outside src/: it fills the store with real
// Hacker News stories so GET /posts can be demonstrated on real data. Not part of
// the assignment, and nothing in the API references it. The API it reads is the
// official, key-less Firebase one.
//
//   dotnet run --project tools/MiniHn.Crawler -- --count 30
//   dotnet run --project tools/MiniHn.Crawler -- --count 5 --dry-run
//   dotnet run --project tools/MiniHn.Crawler -- --db other/minihn.db --clear

var options = CrawlerOptions.Parse(args);
if (options is null)
{
    Console.WriteLine(CrawlerOptions.Usage);
    return 2;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;      // one Ctrl+C stops politely, it does not kill a write
    cancellation.Cancel();
};

using var http = new HttpClient
{
    BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/"),
    Timeout = TimeSpan.FromSeconds(20),
};
http.DefaultRequestHeaders.UserAgent.ParseAdd("MiniHn-Crawler/1.0");

var client = new HackerNewsClient(http);

var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={options.DatabasePath}")
    .Options;

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.DatabasePath))!);

await using var database = new AppDbContext(dbOptions);
await database.Database.MigrateAsync(cancellation.Token);

if (options.Clear)
{
    var removed = await database.Posts.ExecuteDeleteAsync(cancellation.Token);
    Console.WriteLine($"cleared {removed} existing post(s)");
}

// Content is the only column this schema can be deduplicated on, so the stored
// set is read once instead of one EXISTS query per story.
var existing = await database.Posts
    .AsNoTracking()
    .Select(post => post.Content)
    .ToListAsync(cancellation.Token);
var seen = new HashSet<string>(existing, StringComparer.Ordinal);
var storedBefore = seen.Count;   // seen grows as the crawl runs, so a dry run reports from here

Console.WriteLine($"store: {Path.GetFullPath(options.DatabasePath)} ({seen.Count} post(s) already there)");

IReadOnlyList<long> ids;
try
{
    ids = await client.GetTopStoryIdsAsync(cancellation.Token);
}
catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
{
    // "We could not look" is not "there is nothing there": say so and change nothing.
    Console.Error.WriteLine($"hacker news is unreachable: {exception.Message}");
    return 1;
}

Console.WriteLine($"top stories: {ids.Count}, taking up to {options.Count}");

var added = 0;
var skipped = 0;
var failed = 0;
var inspected = 0;

foreach (var id in ids)
{
    if (cancellation.IsCancellationRequested || added >= options.Count)
    {
        break;
    }

    inspected++;

    HackerNewsItem? item;
    try
    {
        item = await client.GetItemAsync(id, cancellation.Token);
    }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
    {
        failed++;
        Console.Error.WriteLine($"  item {id}: {exception.Message}");
        continue;
    }

    if (!PostContent.IsStorable(item))
    {
        skipped++;
        continue;
    }

    var content = PostContent.From(item!);
    if (!seen.Add(content))
    {
        skipped++;
        continue;
    }

    if (options.DryRun)
    {
        Console.WriteLine($"  would add: {Preview(content)}");
        added++;
        continue;
    }

    database.Posts.Add(new Post { Content = content });
    added++;
    Console.WriteLine($"  + {Preview(content)}");

    // One save per ten rows: a crawl interrupted halfway still leaves a
    // consistent store, and a transaction per row is a write per round trip.
    if (added % 10 == 0)
    {
        await database.SaveChangesAsync(cancellation.Token);
    }

    await Task.Delay(options.DelayMilliseconds, cancellation.Token);
}

if (!options.DryRun)
{
    await database.SaveChangesAsync(CancellationToken.None);
}

var total = options.DryRun ? storedBefore : await database.Posts.CountAsync(CancellationToken.None);
Console.WriteLine(
    $"done: inspected {inspected}, added {added}, skipped {skipped}, failed {failed}; store now holds {total} post(s)" +
    (options.DryRun ? " (dry run, nothing was written)" : string.Empty));

return failed > 0 && added == 0 ? 1 : 0;

static string Preview(string content) =>
    content.Length <= 90 ? content : content[..90] + "...";
