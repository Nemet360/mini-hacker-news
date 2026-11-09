using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniHn.Domain;

namespace MiniHn.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Post"/> onto the posts table.</summary>
public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    /// <summary>The upper bound on a post body, in code points.</summary>
    public const int MaxContentLength = 10_000;

    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts", table => table.HasCheckConstraint(
            "ck_posts_content_length",
            $"length(content) BETWEEN 1 AND {MaxContentLength}"));

        builder.HasKey(post => post.Id);

        builder.Property(post => post.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(post => post.Content)
            .HasColumnName("content")
            .IsRequired()
            .HasMaxLength(MaxContentLength);
    }
}
