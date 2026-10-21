using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Results;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence.Configurations;

/// <summary>lifecycle_credit.gpa_snapshots — one per student and term, rebuilt from the credit ledger (P9.3b).</summary>
internal sealed class GpaSnapshotConfiguration : IEntityTypeConfiguration<GpaSnapshot>
{
    public void Configure(EntityTypeBuilder<GpaSnapshot> builder)
    {
        builder.ToTable("gpa_snapshots", table =>
            table.HasCheckConstraint("ck_gpa_snapshots_credits", "credits_attempted >= 0 AND credits_earned >= 0"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.TermGpa).HasPrecision(4, 3);
        builder.Property(s => s.CumulativeGpa).HasPrecision(4, 3);

        builder.HasIndex(s => new { s.StudentId, s.TermId }).IsUnique();
    }
}
