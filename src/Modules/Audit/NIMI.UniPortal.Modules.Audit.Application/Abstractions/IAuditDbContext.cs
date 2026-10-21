using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Audit.Domain;

namespace NIMI.UniPortal.Modules.Audit.Application.Abstractions;

/// <summary>The Audit module's data seam (P10.2). Only the event handler writes; it saves itself.</summary>
public interface IAuditDbContext
{
    DbSet<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
