using Microsoft.EntityFrameworkCore;
using MiniHn.Infrastructure.Persistence;

namespace MiniHn.Api.Features.Posts;

/// <summary>Routes under /posts.</summary>
public static class PostsEndpoints
{
    /// <summary>Maps every posts route onto the application.</summary>
    public static IEndpointRouteBuilder MapPosts(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/posts", GetPosts);
        return endpoints;
    }

    // Ordered by id so the body is deterministic: a reviewer comparing two runs
    // must not see a different array because the planner returned rows in another order.
    private static async Task<IResult> GetPosts(AppDbContext database, CancellationToken cancellationToken)
    {
        var posts = await database.Posts
            .AsNoTracking()
            .OrderBy(post => post.Id)
            .Select(post => new PostResponse(post.Id, post.Content))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(posts);
    }
}
