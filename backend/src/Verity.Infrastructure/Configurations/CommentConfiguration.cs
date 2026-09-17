using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verity.Domain.Entities;

namespace Verity.Infrastructure.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Body).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(c => new { c.PostId, c.CreatedAt })
            .HasDatabaseName("ix_comments_post_created");
    }
}
