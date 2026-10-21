using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.Grades.Infrastructure.Persistence.Configurations;

/// <summary>grades.grade_corrections — the append-only audit of changes to published grades (R4).</summary>
internal sealed class GradeCorrectionConfiguration : IEntityTypeConfiguration<GradeCorrection>
{
    public void Configure(EntityTypeBuilder<GradeCorrection> builder)
    {
        builder.ToTable("grade_corrections");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.HasOne<FinalGrade>()
            .WithMany()
            .HasForeignKey(c => c.FinalGradeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(c => c.OldScore).HasPrecision(5, 2);
        builder.Property(c => c.NewScore).HasPrecision(5, 2);
        builder.Property(c => c.OldGrade).HasMaxLength(10);
        builder.Property(c => c.NewGrade).HasMaxLength(10);
        builder.Property(c => c.Reason).HasMaxLength(GradeCorrection.MaxReasonLength);

        builder.HasIndex(c => c.FinalGradeId);
    }
}
