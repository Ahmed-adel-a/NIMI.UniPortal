using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;

namespace NIMI.UniPortal.Modules.Platform.Application.Abstractions;

/// <summary>The Platform module's single data seam (no repositories — solution-structure.md §6).</summary>
public interface IPlatformDbContext
{
    DbSet<SystemSettings> SystemSettings { get; }

    /// <summary>Needed to set the expected row version (optimistic concurrency) before saving.</summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The settings row's version stamp: PostgreSQL's system column <c>xmin</c>, mapped as a shadow property so the
/// domain stays persistence-free. Clients echo it back on PUT; a mismatch → 409 instead of a silent overwrite.
/// </summary>
public static class SettingsVersion
{
    public const string PropertyName = "Version";
}
