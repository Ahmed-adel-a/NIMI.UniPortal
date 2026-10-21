using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;

/// <summary>The Scheduling module's data seam — the only thing handlers know about persistence.</summary>
public interface ISchedulingDbContext
{
    DbSet<Term> Terms { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
