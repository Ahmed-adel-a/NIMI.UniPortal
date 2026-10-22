using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence.Configurations;

/// <summary>lifecycle_semester.student_standings: one per student; a cohort is a department + level.</summary>
internal sealed class StudentStandingConfiguration : IEntityTypeConfiguration<StudentStanding>
{
    public void Configure(EntityTypeBuilder<StudentStanding> builder)
    {
        builder.ToTable("student_standings", table =>
        {
            table.HasCheckConstraint("ck_student_standings_level", $"level BETWEEN 1 AND {AcademicLevels.Max}");
            table.HasCheckConstraint(
                "ck_student_standings_status",
                "status IN ('active','repeating','external','on_hold','graduated','withdrawn')");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Status).HasConversion(new SnakeCaseEnumConverter<StandingStatus>()).HasMaxLength(20);
        builder.Ignore(s => s.IsEnrolling);
        builder.Ignore(s => s.TakesFailedSubjectsOnly);

        builder.HasIndex(s => s.StudentId).IsUnique();
        builder.HasIndex(s => new { s.DepartmentId, s.Level, s.Status });
    }
}
