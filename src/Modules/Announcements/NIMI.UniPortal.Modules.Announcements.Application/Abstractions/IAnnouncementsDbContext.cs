using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Announcements.Domain;

namespace NIMI.UniPortal.Modules.Announcements.Application.Abstractions;

/// <summary>The Announcements data seam: handlers use this, never a repository or a SaveChanges of their own.</summary>
public interface IAnnouncementsDbContext
{
    DbSet<Announcement> Announcements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
