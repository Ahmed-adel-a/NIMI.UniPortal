using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.Grades.Application.Abstractions;

/// <summary>The Grades module's data seam (P9). Commands change tracked entities; the module's UnitOfWork saves once.</summary>
public interface IGradesDbContext
{
    DbSet<FinalGrade> FinalGrades { get; }

    DbSet<GradeCorrection> GradeCorrections { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
