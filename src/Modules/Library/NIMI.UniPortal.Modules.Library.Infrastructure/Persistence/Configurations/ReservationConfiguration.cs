using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence.Configurations;

/// <summary>library.reservations — one active (waiting/ready) reservation per book and member.</summary>
internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations", table =>
            table.HasCheckConstraint("ck_reservations_status", "status IN ('waiting','ready','fulfilled','cancelled','expired')"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.HasOne<Book>().WithMany().HasForeignKey(r => r.BookId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(r => r.Status).HasConversion(new SnakeCaseEnumConverter<ReservationStatus>()).HasMaxLength(20);
        builder.Ignore(r => r.IsActive);
        builder.HasIndex(r => new { r.BookId, r.UserId })
            .IsUnique()
            .HasFilter("status IN ('waiting','ready')")
            .HasDatabaseName("ux_reservations_active_book_user");
        builder.HasIndex(r => new { r.BookId, r.Status, r.CreatedAt });
        builder.HasIndex(r => r.UserId);
    }
}
