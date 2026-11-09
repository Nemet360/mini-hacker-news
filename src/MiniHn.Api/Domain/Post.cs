namespace MiniHn.Domain;

/// <summary>A single Hacker News style post. Phase I carries an id and the content, nothing else.</summary>
public sealed class Post
{
    /// <summary>Database generated identity. 64 bit from day one: widening the key later is a table rewrite.</summary>
    public long Id { get; set; }

    /// <summary>The post body. Required; the store additionally bounds its length.</summary>
    public required string Content { get; set; }
}
