using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verity.Domain.Entities;

namespace Verity.Infrastructure.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts", t => t.HasCheckConstraint("ck_posts_like_count_non_negative", "like_count >= 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Body).IsRequired();
        builder.Property(p => p.LikeCount).IsRequired().HasDefaultValue(0);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasOne(p => p.Author)
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // The Post -> {Comments,Likes,Tags} relationships are configured from
        // the dependent side (Comment/Like/PostTagAssignment configurations)
        // so each relationship is declared exactly once.
        builder.Navigation(p => p.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Likes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => new { p.CreatedAt, p.Id })
            .IsDescending(true, true)
            .HasDatabaseName("ix_posts_created_at");

        builder.HasIndex(p => new { p.LikeCount, p.CreatedAt, p.Id })
            .IsDescending(true, true, true)
            .HasDatabaseName("ix_posts_like_count");

        builder.HasIndex(p => new { p.AuthorId, p.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_posts_author_created");
    }
}
