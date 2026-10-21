using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Materials.Domain;

namespace NIMI.UniPortal.Modules.Materials.Application.Abstractions;

/// <summary>The Materials data seam: handlers use this, never a repository or a SaveChanges of their own.</summary>
public interface IMaterialsDbContext
{
    DbSet<Material> Materials { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
