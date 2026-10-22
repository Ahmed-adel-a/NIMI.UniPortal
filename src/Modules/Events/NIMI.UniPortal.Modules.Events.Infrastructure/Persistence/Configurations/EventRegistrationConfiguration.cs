using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.Events.Infrastructure.Persistence.Configurations;

/// <summary>events.event_registrations — one row per event and member (R3).</summary>
internal sealed class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
{
    public void Configure(EntityTypeBuilder<EventRegistration> builder)
    {
        builder.ToTable("event_registrations", table =>
            table.HasCheckConstraint("ck_event_registrations_status", "status IN ('registered','cancelled','attended')"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.HasOne<CampusEvent>().WithMany().HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(r => r.Status)
            .HasConversion(s => s.ToString().ToLowerInvariant(), text => Enum.Parse<EventRegistrationStatus>(text, true))
            .HasMaxLength(20);
        builder.HasIndex(r => new { r.EventId, r.UserId }).IsUnique();
        builder.HasIndex(r => r.UserId);
    }
}
