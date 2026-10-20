using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Domain.Organization;

namespace NIMI.UniPortal.Modules.Users.Application.Abstractions;

/// <summary>The Users module's single data seam (no repositories — solution-structure.md §6).</summary>
public interface IUsersDbContext
{
    DbSet<Faculty> Faculties { get; }

    DbSet<Department> Departments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
