using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Grades.Application.Abstractions;
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.Grades.Infrastructure.Persistence;

/// <summary>The Grades module's own schema (P9, schema <c>grades</c>) — ids of other modules only.</summary>
public sealed class GradesDbContext(DbContextOptions<GradesDbContext> options)
    : DbContext(options), IGradesDbContext
{
    public const string Schema = "grades";

    public DbSet<FinalGrade> FinalGrades => Set<FinalGrade>();

    public DbSet<GradeCorrection> GradeCorrections => Set<GradeCorrection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GradesDbContext).Assembly);
    }
}
