using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence.Configurations;

internal sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ToTable("attendance_sessions", table =>
            table.HasCheckConstraint("ck_attendance_sessions_qr_secret", "octet_length(qr_secret) = 32"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.SlotId);
        builder.Property(s => s.SubjectGroupId);
        builder.Property(s => s.SubjectId);
        builder.Property(s => s.TermId);
        builder.Property(s => s.SessionDate);
        builder.Property(s => s.OpenedBy);
        builder.Property(s => s.OpenedAt);
        builder.Property(s => s.ClosedAt);
        builder.Property(s => s.QrSecret).IsRequired();
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(s => new { s.SlotId, s.SessionDate })
            .IsUnique()
            .HasDatabaseName("ux_attendance_sessions_slot_date");
        builder.HasIndex(s => s.SubjectGroupId);
        builder.HasIndex(s => s.TermId);
    }
}
