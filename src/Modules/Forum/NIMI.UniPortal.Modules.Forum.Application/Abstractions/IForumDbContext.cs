using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Application.Abstractions;

/// <summary>The Forum module's data seam (P11). Commands change tracked entities; the module's UnitOfWork saves once.</summary>
public interface IForumDbContext
{
    DbSet<ForumCategory> Categories { get; }

    DbSet<ForumThread> Threads { get; }

    DbSet<ForumPost> Posts { get; }

    DbSet<ForumReport> Reports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
