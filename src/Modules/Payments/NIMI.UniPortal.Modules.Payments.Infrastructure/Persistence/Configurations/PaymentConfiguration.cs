using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Payments.Domain.Fees;
using NIMI.UniPortal.Modules.Payments.Domain.Ledger;
using NIMI.UniPortal.Modules.Payments.Domain.Online;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", table =>
        {
            table.HasCheckConstraint("ck_payments_amount", "amount > 0");
            table.HasCheckConstraint(
                "ck_payments_method",
                "method IN ('cash','bank_transfer','online')");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.StudentFeeId);
        builder.Property(p => p.StudentId);
        builder.Property(p => p.Amount).HasPrecision(12, 2);
        builder.Property(p => p.Method)
            .HasConversion(new SnakeCaseEnumConverter<PaymentMethod>())
            .HasMaxLength(20);
        builder.Property(p => p.Reference).HasMaxLength(100);
        builder.Property(p => p.PaymentIntentId);
        builder.Property(p => p.ReceiptNumber).HasMaxLength(30);
        builder.Property(p => p.RecordedBy);
        builder.Property(p => p.PaidAt);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<StudentFee>()
            .WithMany()
            .HasForeignKey(p => p.StudentFeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PaymentIntent>()
            .WithMany()
            .HasForeignKey(p => p.PaymentIntentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.ReceiptNumber).IsUnique();
        builder.HasIndex(p => new { p.StudentId, p.PaidAt }).IsDescending(false, true);
        builder.HasIndex(p => p.PaymentIntentId)
            .IsUnique()
            .HasFilter("payment_intent_id IS NOT NULL");
    }
}
