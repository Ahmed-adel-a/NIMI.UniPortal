using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Infrastructure.Persistence.Configurations;

/// <summary>lifecycle_semester.year_promotions: the history of year-end decisions.</summary>
internal sealed class YearPromotionConfiguration : IEntityTypeConfiguration<YearPromotion>
{
    public void Configure(EntityTypeBuilder<YearPromotion> builder)
    {
        builder.ToTable("year_promotions", table =>
            table.HasCheckConstraint(
                "ck_year_promotions_result",
                "result IN ('promoted','promoted_with_carried','repeat','external','graduated','held')"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Result).HasConversion(new SnakeCaseEnumConverter<PromotionResult>()).HasMaxLength(30);
        builder.Property(p => p.Reason).HasMaxLength(1000);

        builder.HasIndex(p => new { p.StudentId, p.AcademicYear });
        builder.HasIndex(p => new { p.AcademicYear, p.DepartmentId });
    }
}
