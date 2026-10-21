using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects", table =>
        {
            table.HasCheckConstraint("ck_subjects_credit_hours", "credit_hours BETWEEN 0 AND 12");
            table.HasCheckConstraint("ck_subjects_category", "category IN ('regular','military_education')");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.DepartmentId);
        builder.Property(s => s.Code).HasMaxLength(20);
        builder.Property(s => s.Name).HasMaxLength(255);
        builder.Property(s => s.NameAr).HasMaxLength(255);
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.CreditHours);
        builder.Property(s => s.Category)
            .HasConversion(new SnakeCaseEnumConverter<SubjectCategory>())
            .HasMaxLength(30);
        builder.Property(s => s.IsGraduationRequirement).HasDefaultValue(false);
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.DepartmentId);
    }
}
