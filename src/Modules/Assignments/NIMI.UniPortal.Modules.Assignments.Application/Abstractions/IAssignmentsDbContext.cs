using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.Assignments.Application.Abstractions;

/// <summary>The Assignments data seam: handlers use this, never a repository or a SaveChanges of their own.</summary>
public interface IAssignmentsDbContext
{
    DbSet<Assignment> Assignments { get; }

    DbSet<Submission> Submissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
