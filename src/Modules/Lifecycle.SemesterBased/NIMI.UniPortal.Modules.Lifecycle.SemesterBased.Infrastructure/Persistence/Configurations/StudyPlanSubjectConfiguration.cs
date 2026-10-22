using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence.Configurations;

/// <summary>lifecycle_semester.study_plan_subjects: a subject appears once per plan.</summary>
internal sealed class StudyPlanSubjectConfiguration : IEntityTypeConfiguration<StudyPlanSubject>
{
    public void Configure(EntityTypeBuilder<StudyPlanSubject> builder)
    {
        builder.ToTable("study_plan_subjects", table =>
            table.HasCheckConstraint("ck_study_plan_subjects_term", "term IN ('first','second')"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Term).HasConversion(new SnakeCaseEnumConverter<PlanTerm>()).HasMaxLength(10);
        builder.HasIndex(s => new { s.StudyPlanId, s.SubjectId }).IsUnique();
    }
}
