using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence.Configurations;

internal sealed class CreditLedgerEntryConfiguration : IEntityTypeConfiguration<CreditLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CreditLedgerEntry> builder)
    {
        builder.ToTable("credit_ledger");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.StudentId);
        builder.Property(e => e.TermId);
        builder.Property(e => e.SubjectId);
        builder.Property(e => e.CreditHours);
        builder.Property(e => e.Passed);
        builder.Property(e => e.GradePoints).HasPrecision(3, 2);
        builder.Property(e => e.RecordedAt);

        builder.HasIndex(e => new { e.StudentId, e.TermId, e.SubjectId }).IsUnique();
    }
}
