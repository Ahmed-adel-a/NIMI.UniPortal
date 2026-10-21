using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.Assignments.Infrastructure.Persistence.Configurations;

/// <summary>assignments.submissions — one per student per assignment (R4), receipts unique (R6).</summary>
internal sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions", table =>
        {
            table.HasCheckConstraint("ck_submissions_status", "status IN ('submitted','approved','rejected')");
            table.HasCheckConstraint("ck_submissions_size_bytes", "size_bytes > 0");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.HasOne<Assignment>()
            .WithMany()
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(s => s.StudentName).HasMaxLength(255);
        builder.Property(s => s.StudentCode).HasMaxLength(50);
        builder.Property(s => s.StorageKey).HasMaxLength(200);
        builder.Property(s => s.FileName).HasMaxLength(255);
        builder.Property(s => s.ContentType).HasMaxLength(100);
        builder.Property(s => s.ReceiptNumber).HasMaxLength(30);
        builder.Property(s => s.Status)
            .HasConversion(status => status.ToString().ToLowerInvariant(), text => Enum.Parse<SubmissionStatus>(text, true))
            .HasMaxLength(20);
        builder.Property(s => s.Grade).HasPrecision(5, 2);
        builder.Property(s => s.Feedback).HasMaxLength(4000);

        builder.HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();
        builder.HasIndex(s => s.ReceiptNumber).IsUnique();
        builder.HasIndex(s => new { s.StudentId, s.SubmittedAt });
    }
}
