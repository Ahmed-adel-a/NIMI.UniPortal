using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Notifications.Domain;

namespace NIMI.UniPortal.Modules.Notifications.Application.Abstractions;

/// <summary>The Notifications module's data seam — the only thing handlers know about persistence.</summary>
public interface INotificationsDbContext
{
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
