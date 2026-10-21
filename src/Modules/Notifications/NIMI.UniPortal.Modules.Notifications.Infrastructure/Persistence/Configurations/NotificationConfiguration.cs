using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Notifications.Domain;

namespace NIMI.UniPortal.Modules.Notifications.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", table => table.HasCheckConstraint(
            "ck_notifications_type",
            "type IN ('schedule','assignment','grade','attendance','announcement','payment','forum','event','system')"));

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.UserId);
        builder.Property(n => n.Type)
            .HasConversion(new LowerCaseEnumConverter<NotificationType>())
            .HasMaxLength(20);
        builder.Property(n => n.Title).HasMaxLength(200);
        builder.Property(n => n.TitleAr).HasMaxLength(200);
        builder.Property(n => n.Message).HasMaxLength(2000);
        builder.Property(n => n.MessageAr).HasMaxLength(2000);
        builder.Property(n => n.ActionUrl).HasMaxLength(500);
        builder.Property(n => n.SourceEventId);
        builder.Property(n => n.IsRead).HasDefaultValue(false);
        builder.Property(n => n.ReadAt);
        builder.Property(n => n.CreatedAt).HasDefaultValueSql("now()");

        // Rule R3: one notification per recipient per event, enforced by the database too.
        builder.HasIndex(n => new { n.SourceEventId, n.UserId }).IsUnique();

        // The unread badge / "my notifications" reads.
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt }).IsDescending(false, false, true);
    }
}
