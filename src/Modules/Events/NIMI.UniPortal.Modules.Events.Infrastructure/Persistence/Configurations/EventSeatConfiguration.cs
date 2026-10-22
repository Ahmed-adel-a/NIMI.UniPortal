using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.Events.Infrastructure.Persistence.Configurations;

/// <summary>events.event_seats — the atomic counter (R2); the database itself refuses an over-booking.</summary>
internal sealed class EventSeatConfiguration : IEntityTypeConfiguration<EventSeat>
{
    public void Configure(EntityTypeBuilder<EventSeat> builder)
    {
        builder.ToTable("event_seats", table =>
            table.HasCheckConstraint("ck_event_seats_taken", "taken >= 0 AND taken <= capacity"));
        builder.HasKey(s => s.EventId);
        builder.Property(s => s.EventId).ValueGeneratedNever();
        builder.HasOne<CampusEvent>().WithOne().HasForeignKey<EventSeat>(s => s.EventId).OnDelete(DeleteBehavior.Restrict);
    }
}
