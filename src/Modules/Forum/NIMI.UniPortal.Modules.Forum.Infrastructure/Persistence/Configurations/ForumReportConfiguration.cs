using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Infrastructure.Persistence.Configurations;

/// <summary>forum.reports — exactly one target; the open queue is read oldest first.</summary>
internal sealed class ForumReportConfiguration : IEntityTypeConfiguration<ForumReport>
{
    public void Configure(EntityTypeBuilder<ForumReport> builder)
    {
        builder.ToTable("reports", table =>
        {
            table.HasCheckConstraint("ck_reports_one_target", "(thread_id IS NULL) <> (post_id IS NULL)");
            table.HasCheckConstraint("ck_reports_status", "status IN ('open','resolved')");
            table.HasCheckConstraint("ck_reports_resolution", "resolution IS NULL OR resolution IN ('removed','dismissed')");
        });
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.HasOne<ForumThread>().WithMany().HasForeignKey(r => r.ThreadId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ForumPost>().WithMany().HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(r => r.Reason).HasMaxLength(ForumReport.MaxReasonLength);
        builder.Property(r => r.Status)
            .HasConversion(s => s.ToString().ToLowerInvariant(), text => Enum.Parse<ForumReportStatus>(text, true))
            .HasMaxLength(20);
        builder.Property(r => r.Resolution)
            .HasConversion(
                r => r == null ? null : r.Value.ToString().ToLowerInvariant(),
                text => text == null ? null : Enum.Parse<ForumReportResolution>(text, true))
            .HasMaxLength(20);
        builder.HasIndex(r => new { r.Status, r.CreatedAt });
    }
}
