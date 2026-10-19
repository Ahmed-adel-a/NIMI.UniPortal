using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;

namespace NIMI.UniPortal.Modules.Platform.Application.Abstractions;

/// <summary>The Platform module's single data seam (no repositories — solution-structure.md §6).</summary>
public interface IPlatformDbContext
{
    DbSet<SystemSettings> SystemSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
