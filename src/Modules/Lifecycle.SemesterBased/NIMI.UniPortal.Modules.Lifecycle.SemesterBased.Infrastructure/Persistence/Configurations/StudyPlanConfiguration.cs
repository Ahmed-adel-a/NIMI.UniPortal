using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence.Configurations;

/// <summary>lifecycle_semester.study_plans: one per department; its subjects are replaced as a whole.</summary>
internal sealed class StudyPlanConfiguration : IEntityTypeConfiguration<StudyPlan>
{
    public void Configure(EntityTypeBuilder<StudyPlan> builder)
    {
        builder.ToTable("study_plans", table =>
            table.HasCheckConstraint("ck_study_plans_levels", $"levels BETWEEN 1 AND {AcademicLevels.Max}"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.HasIndex(p => p.DepartmentId).IsUnique();

        builder.HasMany(p => p.Subjects)
            .WithOne()
            .HasForeignKey(s => s.StudyPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Subjects).HasField("_subjects").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
