namespace MiniHn.Crawler.Tests;

public sealed class PostContentTests
{
    private static HackerNewsItem Story(
        string? title = "A title",
        string? url = "https://example.com/a",
        string? text = null,
        string? type = "story",
        bool? deleted = null,
        bool? dead = null) =>
        new(1, type, title, url, text, "someone", 10, deleted, dead);

    [Fact]
    public void A_live_story_with_a_title_is_storable()
    {
        PostContent.IsStorable(Story()).Should().BeTrue();
    }

    [Theory]
    [InlineData("comment")]
    [InlineData("job")]
    [InlineData("poll")]
    [InlineData(null)]
    public void Only_stories_are_storable(string? type)
    {
        PostContent.IsStorable(Story(type: type)).Should().BeFalse();
    }

    [Fact]
    public void Deleted_dead_titleless_and_null_items_are_not_storable()
    {
        PostContent.IsStorable(Story(deleted: true)).Should().BeFalse();
        PostContent.IsStorable(Story(dead: true)).Should().BeFalse();
        PostContent.IsStorable(Story(title: "   ")).Should().BeFalse();
        PostContent.IsStorable(null).Should().BeFalse();
    }

    [Fact]
    public void Content_is_the_title_and_the_link()
    {
        var content = PostContent.From(Story(title: "Rust 2.0 released", url: "https://rust-lang.org"));

        content.Should().Be("Rust 2.0 released — https://rust-lang.org");
    }

    [Fact]
    public void A_text_only_post_carries_its_body_without_markup()
    {
        var item = Story(
            title: "Ask HN: what do you use?",
            url: null,
            text: "<p>I use <i>vim</i> &amp; tmux.</p>");

        var content = PostContent.From(item);

        content.Should().Be("Ask HN: what do you use? — I use vim & tmux.");
    }

    [Fact]
    public void Content_never_exceeds_the_schema_bound()
    {
        var item = Story(title: new string('x', 12_000), url: null);

        var content = PostContent.From(item);

        content.Length.Should().Be(PostContent.MaxLength);
    }

    [Fact]
    public void Truncation_never_splits_a_surrogate_pair()
    {
        var emoji = string.Concat(Enumerable.Repeat("\U0001F600", 10));   // 2 chars each

        var cut = PostContent.Truncate(emoji, 5);

        cut.Length.Should().Be(4, "the odd character would have been half of a pair");
        char.IsLowSurrogate(cut[^1]).Should().BeTrue();
    }

    [Fact]
    public void Stripping_collapses_whitespace_and_decodes_entities()
    {
        PostContent.StripHtml("<p>a   b<br>&lt;tag&gt;</p>").Should().Be("a b <tag>");
    }
}
