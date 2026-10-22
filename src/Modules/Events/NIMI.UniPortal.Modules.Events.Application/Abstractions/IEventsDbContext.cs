using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.Events.Application.Abstractions;

/// <summary>The Events module's data seam (P11). Commands change tracked entities; the module's UnitOfWork saves once.</summary>
public interface IEventsDbContext
{
    DbSet<CampusEvent> Events { get; }

    DbSet<EventRegistration> Registrations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
