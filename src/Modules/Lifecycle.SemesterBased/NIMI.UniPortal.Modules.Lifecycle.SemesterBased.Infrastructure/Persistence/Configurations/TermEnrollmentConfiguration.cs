using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence.Configurations;

/// <summary>lifecycle_semester.term_enrollments: the rosters of the semester-based variant.</summary>
internal sealed class TermEnrollmentConfiguration : IEntityTypeConfiguration<TermEnrollment>
{
    public void Configure(EntityTypeBuilder<TermEnrollment> builder)
    {
        builder.ToTable("term_enrollments", table =>
            table.HasCheckConstraint("ck_term_enrollments_status", "status IN ('enrolled','passed','failed','withdrawn')"));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Status).HasConversion(new SnakeCaseEnumConverter<EnrollmentStatus>()).HasMaxLength(20);
        builder.Property(e => e.GradePoints).HasPrecision(4, 2);

        // One live enrollment per student, term and subject (generation is idempotent on it).
        builder.HasIndex(e => new { e.StudentId, e.TermId, e.SubjectId })
            .IsUnique()
            .HasFilter("status <> 'withdrawn'");
        builder.HasIndex(e => new { e.SubjectGroupId, e.Status });
    }
}
