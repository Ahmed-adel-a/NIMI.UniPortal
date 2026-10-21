using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.Grades.Infrastructure.Persistence.Configurations;

/// <summary>grades.final_grades — one per student, subject and term; optimistic concurrency on xmin.</summary>
internal sealed class FinalGradeConfiguration : IEntityTypeConfiguration<FinalGrade>
{
    public void Configure(EntityTypeBuilder<FinalGrade> builder)
    {
        builder.ToTable("final_grades", table =>
        {
            table.HasCheckConstraint("ck_final_grades_status", "status IN ('draft','confirmed','published')");
            table.HasCheckConstraint("ck_final_grades_score", "score >= 0 AND score <= 100");
            table.HasCheckConstraint("ck_final_grades_credit_hours", "credit_hours >= 0");
        });

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.Score).HasPrecision(5, 2);
        builder.Property(g => g.Grade).HasMaxLength(10);
        builder.Property(g => g.GradePoints).HasPrecision(3, 2);
        builder.Property(g => g.Status)
            .HasConversion(status => status.ToString().ToLowerInvariant(), text => Enum.Parse<GradeStatus>(text, true))
            .HasMaxLength(20);
        builder.Ignore(g => g.Passed);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(g => new { g.StudentId, g.SubjectId, g.TermId }).IsUnique();
        builder.HasIndex(g => new { g.TermId, g.Status });
        builder.HasIndex(g => g.SubjectGroupId);
    }
}
