using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Verity.Domain.Entities;

namespace Verity.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username).HasMaxLength(32).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();

        // Case-insensitive uniqueness is enforced via a stored, generated
        // lower(...) column so the constraint lives in Postgres, not just in C#.
        builder.Property<string>("UsernameLower")
            .HasComputedColumnSql("lower(username)", stored: true);
        builder.HasIndex("UsernameLower").IsUnique().HasDatabaseName("ix_users_username_lower");

        builder.Property<string>("EmailLower")
            .HasComputedColumnSql("lower(email)", stored: true);
        builder.HasIndex("EmailLower").IsUnique().HasDatabaseName("ix_users_email_lower");
    }
}
