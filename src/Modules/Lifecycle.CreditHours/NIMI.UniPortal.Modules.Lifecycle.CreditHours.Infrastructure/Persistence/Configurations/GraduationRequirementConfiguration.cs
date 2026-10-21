using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Graduation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence.Configurations;

/// <summary>lifecycle_credit.graduation_requirements — decision GRADREQ, one row per department.</summary>
internal sealed class GraduationRequirementConfiguration : IEntityTypeConfiguration<GraduationRequirement>
{
    public void Configure(EntityTypeBuilder<GraduationRequirement> builder)
    {
        builder.ToTable("graduation_requirements", table =>
            table.HasCheckConstraint(
                "ck_graduation_requirements_credits",
                $"required_credits > 0 AND required_credits <= {GraduationRequirement.MaxCredits}"));

        builder.HasKey(r => r.DepartmentId);
        builder.Property(r => r.DepartmentId).ValueGeneratedNever();
    }
}
