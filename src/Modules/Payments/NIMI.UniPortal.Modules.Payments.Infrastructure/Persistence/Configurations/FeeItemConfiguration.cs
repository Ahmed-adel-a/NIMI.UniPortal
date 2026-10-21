using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence.Configurations;

internal sealed class FeeItemConfiguration : IEntityTypeConfiguration<FeeItem>
{
    public void Configure(EntityTypeBuilder<FeeItem> builder)
    {
        builder.ToTable("fee_items", table =>
        {
            table.HasCheckConstraint(
                "ck_fee_items_category",
                "category IN ('tuition','military_education','activity','other')");
            table.HasCheckConstraint("ck_fee_items_amount", "amount > 0");
        });

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Name).HasMaxLength(255);
        builder.Property(f => f.NameAr).HasMaxLength(255);
        builder.Property(f => f.Description).HasMaxLength(2000);
        builder.Property(f => f.Category)
            .HasConversion(new SnakeCaseEnumConverter<FeeCategory>())
            .HasMaxLength(30);
        builder.Property(f => f.Amount).HasPrecision(12, 2);
        builder.Property(f => f.TermId);
        builder.Property(f => f.FacultyId);
        builder.Property(f => f.DepartmentId);
        builder.Property(f => f.DueOn);
        builder.Property(f => f.IsActive).HasDefaultValue(true);
        builder.Property(f => f.CreatedBy);
        builder.Property(f => f.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(f => f.UpdatedAt);

        builder.HasIndex(f => f.TermId);
        builder.HasIndex(f => f.FacultyId);
        builder.HasIndex(f => f.DepartmentId);
        builder.HasIndex(f => f.IsActive);
    }
}
