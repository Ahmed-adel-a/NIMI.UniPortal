using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Payments.Domain.Online;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence.Configurations;

internal sealed class PaymentIntentConfiguration : IEntityTypeConfiguration<PaymentIntent>
{
    public void Configure(EntityTypeBuilder<PaymentIntent> builder)
    {
        builder.ToTable("payment_intents", table =>
        {
            table.HasCheckConstraint("ck_payment_intents_amount", "amount > 0");
            table.HasCheckConstraint(
                "ck_payment_intents_status",
                "status IN ('pending','succeeded','failed','expired')");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.StudentId);
        builder.Property(i => i.FeeItemId);
        builder.Property(i => i.Amount).HasPrecision(12, 2);
        builder.Property(i => i.Provider).HasMaxLength(30);
        builder.Property(i => i.ProviderReference).HasMaxLength(100);
        builder.Property(i => i.Status)
            .HasConversion(new SnakeCaseEnumConverter<PaymentIntentStatus>())
            .HasMaxLength(20);
        builder.Property(i => i.FailureReason).HasMaxLength(500);
        builder.Property(i => i.ExpiresAt);
        builder.Property(i => i.CompletedAt);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(i => i.UpdatedAt);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(i => new { i.Provider, i.ProviderReference }).IsUnique();
        builder.HasIndex(i => i.StudentId);
        builder.HasIndex(i => i.FeeItemId);
        builder.HasIndex(i => i.ExpiresAt);
    }
}
