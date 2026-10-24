using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Abstractions;

/// <summary>The StudentReports data seam: handlers use this, never a repository or a SaveChanges of their own.</summary>
public interface IStudentReportsDbContext
{
    DbSet<StudentReport> Reports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
