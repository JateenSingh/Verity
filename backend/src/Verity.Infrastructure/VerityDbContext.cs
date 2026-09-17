using Microsoft.EntityFrameworkCore;
using Verity.Application.Data;
using Verity.Domain.Entities;

namespace Verity.Infrastructure;

public sealed class VerityDbContext(DbContextOptions<VerityDbContext> options) : DbContext(options), IVerityDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<PostTagAssignment> PostTags => Set<PostTagAssignment>();

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var strategy = Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VerityDbContext).Assembly);
    }
}
