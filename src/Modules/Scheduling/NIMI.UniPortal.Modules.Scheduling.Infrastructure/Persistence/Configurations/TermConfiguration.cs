using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class TermConfiguration : IEntityTypeConfiguration<Term>
{
    public void Configure(EntityTypeBuilder<Term> builder)
    {
        builder.ToTable("terms", table =>
        {
            table.HasCheckConstraint("ck_terms_number", "number IN ('first','second','summer')");
            table.HasCheckConstraint("ck_terms_status", "status IN ('planned','active','closed')");
            table.HasCheckConstraint("ck_terms_dates", "ends_on > starts_on");
            table.HasCheckConstraint("ck_terms_week_count", "week_count BETWEEN 1 AND 30");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.AcademicYear);
        builder.Property(t => t.Number)
            .HasConversion(new SnakeCaseEnumConverter<TermNumber>())
            .HasMaxLength(10);
        builder.Property(t => t.StartsOn);
        builder.Property(t => t.EndsOn);
        builder.Property(t => t.WeekCount);
        builder.Property(t => t.Status)
            .HasConversion(new SnakeCaseEnumConverter<TermStatus>())
            .HasMaxLength(10);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt);

        builder.HasIndex(t => new { t.AcademicYear, t.Number }).IsUnique();
    }
}
