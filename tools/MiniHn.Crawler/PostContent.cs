using System.Net;
using System.Text.RegularExpressions;

namespace MiniHn.Crawler;

/// <summary>
/// Turns a Hacker News item into the single <c>content</c> string the Phase I
/// schema stores. Pure, so this is the part worth testing.
/// </summary>
public static partial class PostContent
{
    /// <summary>The store's bound, mirrored so a crawl never builds a row the database refuses.</summary>
    public const int MaxLength = 10_000;

    /// <summary>The separator between the title, the link and the body.</summary>
    private const string Separator = " — ";

    /// <summary>True when the item is a live story worth storing.</summary>
    public static bool IsStorable(HackerNewsItem? item) =>
        item is not null
        && item.Deleted != true
        && item.Dead != true
        && string.Equals(item.Type, "story", StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(item.Title);

    /// <summary>
    /// Builds the content line: the title, the link when there is one, and the body
    /// of an Ask HN post with its markup stripped. Truncated to the schema bound on
    /// a character boundary, never through a surrogate pair.
    /// </summary>
    public static string From(HackerNewsItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var parts = new List<string> { item.Title!.Trim() };

        if (!string.IsNullOrWhiteSpace(item.Url))
        {
            parts.Add(item.Url.Trim());
        }

        if (!string.IsNullOrWhiteSpace(item.Text))
        {
            parts.Add(StripHtml(item.Text));
        }

        return Truncate(string.Join(Separator, parts), MaxLength);
    }

    /// <summary>Removes the small HTML subset Hacker News allows in a post body.</summary>
    public static string StripHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        // A tag becomes a space, not nothing: "a<br>b" is two words, and the
        // whitespace collapse below removes whatever that over-produces.
        var withoutTags = TagPattern().Replace(html, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespacePattern().Replace(decoded, " ").Trim();
    }

    /// <summary>Cuts to <paramref name="max"/> characters without splitting a surrogate pair.</summary>
    public static string Truncate(string value, int max)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length <= max)
        {
            return value;
        }

        var cut = max;
        if (char.IsHighSurrogate(value[cut - 1]))
        {
            cut--;
        }

        return value[..cut];
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
