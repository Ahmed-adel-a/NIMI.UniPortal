using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Notifications.Domain;

namespace NIMI.UniPortal.Modules.Notifications.Infrastructure.Persistence.Configurations;

/// <summary>notifications.outbound_emails: the email outbox, one row per EmailRequested event.</summary>
internal sealed class OutboundEmailConfiguration : IEntityTypeConfiguration<OutboundEmail>
{
    public void Configure(EntityTypeBuilder<OutboundEmail> builder)
    {
        builder.ToTable("outbound_emails");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.To).HasMaxLength(320);
        builder.Property(e => e.Subject).HasMaxLength(300);
        builder.Property(e => e.Provider).HasMaxLength(40);

        builder.HasIndex(e => e.SourceEventId).IsUnique();
        builder.HasIndex(e => new { e.To, e.CreatedAt });
    }
}
