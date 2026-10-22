using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence.Configurations;

/// <summary>lifecycle_semester.promotion_policies: one university-wide row (no faculty) and one per overriding faculty.</summary>
internal sealed class PromotionPolicyConfiguration : IEntityTypeConfiguration<PromotionPolicy>
{
    public void Configure(EntityTypeBuilder<PromotionPolicy> builder)
    {
        builder.ToTable("promotion_policies", table =>
        {
            table.HasCheckConstraint(
                "ck_promotion_policies_max_carried",
                $"max_carried_subjects BETWEEN 0 AND {PromotionPolicy.MaxCarriedLimit}");
            table.HasCheckConstraint(
                "ck_promotion_policies_max_years",
                $"max_years_per_level BETWEEN 1 AND {PromotionPolicy.MaxYearsLimit}");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.GateLevels);
        builder.Ignore(p => p.Rules);

        // NULLS NOT DISTINCT: the university-wide row (null faculty) is unique too.
        builder.HasIndex(p => p.FacultyId).IsUnique().AreNullsDistinct(false);
    }
}
