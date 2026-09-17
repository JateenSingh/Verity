using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verity.Domain.Entities;

namespace Verity.Infrastructure.Configurations;

public sealed class PostTagAssignmentConfiguration : IEntityTypeConfiguration<PostTagAssignment>
{
    public void Configure(EntityTypeBuilder<PostTagAssignment> builder)
    {
        builder.ToTable("post_tags");

        // A post carries a given tag at most once.
        builder.HasKey(t => new { t.PostId, t.Tag });

        builder.Property(t => t.Reason).HasMaxLength(500);
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasOne(t => t.Post)
            .WithMany(p => p.Tags)
            .HasForeignKey(t => t.PostId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(t => t.TaggedBy)
            .WithMany()
            .HasForeignKey(t => t.TaggedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(t => new { t.Tag, t.PostId }).HasDatabaseName("ix_post_tags_tag");
    }
}
