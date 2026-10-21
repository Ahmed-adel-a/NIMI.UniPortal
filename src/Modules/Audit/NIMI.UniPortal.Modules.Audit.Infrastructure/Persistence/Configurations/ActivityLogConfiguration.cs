using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Audit.Domain;

namespace NIMI.UniPortal.Modules.Audit.Infrastructure.Persistence.Configurations;

/// <summary>audit.activity_logs — append-only; indexes for the three filters the admin view uses.</summary>
internal sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Action).HasMaxLength(ActivityLog.MaxActionLength);
        builder.Property(l => l.EntityType).HasMaxLength(ActivityLog.MaxEntityTypeLength);
        builder.Property(l => l.Summary).HasMaxLength(ActivityLog.MaxSummaryLength);

        builder.HasIndex(l => l.SourceEventId).IsUnique();
        builder.HasIndex(l => l.OccurredAt).IsDescending();
        builder.HasIndex(l => new { l.EntityType, l.EntityId });
        builder.HasIndex(l => l.ActorId);
    }
}
