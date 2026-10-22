using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;

namespace NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence.Configurations;

/// <summary>platform.academic_system_rules: one row for the university, per overriding faculty and per overriding department.</summary>
internal sealed class AcademicSystemRuleConfiguration : IEntityTypeConfiguration<AcademicSystemRule>
{
    public void Configure(EntityTypeBuilder<AcademicSystemRule> builder)
    {
        builder.ToTable("academic_system_rules", table =>
        {
            table.HasCheckConstraint("ck_academic_system_rules_system", "system IN ('credit_hours','year_groups')");
            table.HasCheckConstraint("ck_academic_system_rules_scope", "faculty_id IS NULL OR department_id IS NULL");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.System)
            .HasConversion(
                v => JsonNamingPolicy.SnakeCaseLower.ConvertName(v.ToString()),
                v => Enum.Parse<AcademicSystem>(v.Replace("_", string.Empty, StringComparison.Ordinal), true))
            .HasMaxLength(20);

        // One rule per level; NULLS NOT DISTINCT makes the university row (both null) unique too.
        builder.HasIndex(r => new { r.FacultyId, r.DepartmentId }).IsUnique().AreNullsDistinct(false);
    }
}
