using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.Events.Infrastructure.Persistence.Configurations;

/// <summary>events.events — schedule checks mirrored in SQL; xmin for concurrent edits.</summary>
internal sealed class CampusEventConfiguration : IEntityTypeConfiguration<CampusEvent>
{
    public void Configure(EntityTypeBuilder<CampusEvent> builder)
    {
        builder.ToTable("events", table =>
        {
            table.HasCheckConstraint("ck_events_status", "status IN ('draft','published','cancelled')");
            table.HasCheckConstraint("ck_events_schedule", "ends_at > starts_at AND registration_deadline <= starts_at");
            table.HasCheckConstraint("ck_events_capacity", "capacity IS NULL OR capacity > 0");
        });
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).HasMaxLength(EventText.MaxTitleLength);
        builder.Property(e => e.Description).HasMaxLength(EventText.MaxDescriptionLength);
        builder.Property(e => e.Venue).HasMaxLength(EventText.MaxVenueLength);
        builder.Property(e => e.CancellationReason).HasMaxLength(EventText.MaxReasonLength);
        builder.Property(e => e.Status)
            .HasConversion(s => s.ToString().ToLowerInvariant(), text => Enum.Parse<EventStatus>(text, true))
            .HasMaxLength(20);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => new { e.Status, e.StartsAt });
        builder.HasIndex(e => e.FacultyId);
    }
}
