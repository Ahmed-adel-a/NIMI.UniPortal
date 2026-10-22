using NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;

namespace NIMI.UniPortal.Modules.Platform.Application.Abstractions;

/// <summary>The Platform module's single data seam (no repositories — solution-structure.md §6).</summary>
public interface IPlatformDbContext
{
    DbSet<SystemSettings> SystemSettings { get; }

    DbSet<FacultySettings> FacultySettings { get; }

    DbSet<AcademicSystemRule> AcademicSystemRules { get; }

    /// <summary>Needed to set the expected row version (optimistic concurrency) before saving.</summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
