using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Announcements.Domain;

namespace NIMI.UniPortal.Modules.Announcements.Infrastructure.Persistence.Configurations;

/// <summary>announcements.announcements (phase §5).</summary>
internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("announcements", table =>
            table.HasCheckConstraint("ck_announcements_priority", "priority IN ('normal','high','urgent')"));

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Title).HasMaxLength(255);
        builder.Property(a => a.Body).HasMaxLength(10000);
        builder.Property(a => a.Priority)
            .HasConversion(priority => priority.ToString().ToLowerInvariant(), text => Enum.Parse<AnnouncementPriority>(text, true))
            .HasMaxLength(10);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(a => new { a.SubjectId, a.TermId });
    }
}
