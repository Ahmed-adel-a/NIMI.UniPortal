using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NIMI.UniPortal.Modules.Assignments.Infrastructure.Persistence.Configurations;

/// <summary>One row per receipt year; the allocator updates it atomically (rule R6).</summary>
internal sealed class ReceiptCounterConfiguration : IEntityTypeConfiguration<ReceiptCounter>
{
    public void Configure(EntityTypeBuilder<ReceiptCounter> builder)
    {
        builder.ToTable("receipt_counters", table =>
            table.HasCheckConstraint("ck_receipt_counters_last_value", "last_value >= 0"));

        builder.HasKey(c => c.Year);
        builder.Property(c => c.Year).ValueGeneratedNever();
        builder.Property(c => c.LastValue);
    }
}
