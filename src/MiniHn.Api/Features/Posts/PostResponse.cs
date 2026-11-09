namespace MiniHn.Api.Features.Posts;

/// <summary>The wire shape of a post. Separate from the entity so the schema can change without changing the contract.</summary>
/// <param name="Id">Store assigned identity.</param>
/// <param name="Content">The post body.</param>
public sealed record PostResponse(long Id, string Content);
