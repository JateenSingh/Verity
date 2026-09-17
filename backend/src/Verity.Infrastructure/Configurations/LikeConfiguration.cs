using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verity.Domain.Entities;

namespace Verity.Infrastructure.Configurations;

public sealed class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("likes");

        // One like per user per post, enforced by the database itself.
        builder.HasKey(l => new { l.PostId, l.UserId });

        builder.Property(l => l.CreatedAt).IsRequired();

        builder.HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(l => l.UserId).HasDatabaseName("ix_likes_user");
    }
}
