using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence.Configurations;

internal sealed class SubjectPrerequisiteConfiguration : IEntityTypeConfiguration<SubjectPrerequisite>
{
    public void Configure(EntityTypeBuilder<SubjectPrerequisite> builder)
    {
        builder.ToTable("subject_prerequisites", table =>
            table.HasCheckConstraint(
                "ck_subject_prerequisites_not_self", "subject_id <> prerequisite_subject_id"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.SubjectId);
        builder.Property(p => p.PrerequisiteSubjectId);

        builder.HasIndex(p => new { p.SubjectId, p.PrerequisiteSubjectId }).IsUnique();
    }
}
