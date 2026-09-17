using Microsoft.EntityFrameworkCore;
using Verity.Domain.Entities;

namespace Verity.Application.Data;

public interface IVerityDbContext
{
    DbSet<User> Users { get; }
    DbSet<Post> Posts { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Like> Likes { get; }
    DbSet<PostTagAssignment> PostTags { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="action"/> inside a transaction via the configured
    /// (retrying) execution strategy - EF Core requires user-managed
    /// transactions to go through the strategy when retry-on-failure is on,
    /// rather than a plain Database.BeginTransactionAsync.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken);
}
