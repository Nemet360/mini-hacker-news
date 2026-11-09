using MiniHn.Domain;

namespace MiniHn.Api.Tests;

public sealed class PostTests
{
    [Fact]
    public void Post_requires_content_and_defaults_id_to_zero()
    {
        var post = new Post { Content = "first post" };

        post.Id.Should().Be(0, "the store assigns the identity, never the caller");
        post.Content.Should().Be("first post");
    }
}
