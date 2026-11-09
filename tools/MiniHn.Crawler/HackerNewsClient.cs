using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MiniHn.Crawler;

/// <summary>A story as the public Hacker News API returns it.</summary>
/// <param name="Id">The HN item id.</param>
/// <param name="Type">"story", "comment", "job", "poll" or "pollopt".</param>
/// <param name="Title">The headline.</param>
/// <param name="Url">The linked article, absent on text-only posts.</param>
/// <param name="Text">The body, present on Ask HN style posts.</param>
/// <param name="By">The submitter.</param>
/// <param name="Score">Points at the time of the read.</param>
/// <param name="Deleted">Set on removed items.</param>
/// <param name="Dead">Set on killed items.</param>
public sealed record HackerNewsItem(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("by")] string? By,
    [property: JsonPropertyName("score")] int? Score,
    [property: JsonPropertyName("deleted")] bool? Deleted,
    [property: JsonPropertyName("dead")] bool? Dead);

/// <summary>
/// Reads the official Hacker News Firebase API. No key, no scraping: the public
/// endpoints are documented at https://github.com/HackerNews/API and are the
/// polite way to take this data.
/// </summary>
public sealed class HackerNewsClient(HttpClient http)
{
    /// <summary>Ids of the current top stories, in Hacker News order.</summary>
    public async Task<IReadOnlyList<long>> GetTopStoryIdsAsync(CancellationToken cancellationToken)
    {
        var ids = await http.GetFromJsonAsync<long[]>("topstories.json", cancellationToken);
        return ids ?? [];
    }

    /// <summary>One item, or null when the API answers with JSON null (a purged id).</summary>
    public Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken cancellationToken) =>
        http.GetFromJsonAsync<HackerNewsItem>($"item/{id}.json", cancellationToken);
}
