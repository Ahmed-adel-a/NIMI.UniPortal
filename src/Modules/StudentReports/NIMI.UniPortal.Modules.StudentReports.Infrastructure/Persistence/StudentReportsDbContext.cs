using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.StudentReports.Application.Abstractions;
using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.StudentReports.Infrastructure.Persistence;

/// <summary>The StudentReports module's own schema (decision ROLES2, schema <c>student_reports</c>) — ids of other modules only.</summary>
public sealed class StudentReportsDbContext(DbContextOptions<StudentReportsDbContext> options)
    : DbContext(options), IStudentReportsDbContext
{
    public const string Schema = "student_reports";

    public DbSet<StudentReport> Reports => Set<StudentReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudentReportsDbContext).Assembly);
    }
}
