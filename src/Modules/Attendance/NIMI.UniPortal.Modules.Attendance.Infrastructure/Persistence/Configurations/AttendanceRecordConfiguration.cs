using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence.Configurations;

internal sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records", table =>
        {
            table.HasCheckConstraint(
                "ck_attendance_records_status",
                "status IN ('present','late','manual_audit','absent')");
            table.HasCheckConstraint(
                "ck_attendance_records_method",
                "method IN ('qr_scan','manual_entry','auto_absent')");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.SessionId);
        builder.Property(r => r.StudentId);
        builder.Property(r => r.Status)
            .HasConversion(new SnakeCaseEnumConverter<AttendanceStatus>())
            .HasMaxLength(20);
        builder.Property(r => r.Method)
            .HasConversion(new SnakeCaseEnumConverter<AttendanceMethod>())
            .HasMaxLength(20);
        builder.Property(r => r.CheckedInAt);
        builder.Property(r => r.DeviceFingerprint).HasMaxLength(200);
        builder.Property(r => r.RecordedBy);
        builder.Property(r => r.Note).HasMaxLength(500);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedAt);

        builder.HasOne<AttendanceSession>()
            .WithMany()
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // One record per student per session: the security boundary behind rule R4.
        builder.HasIndex(r => new { r.SessionId, r.StudentId })
            .IsUnique()
            .HasDatabaseName("ux_attendance_records_session_student");
        builder.HasIndex(r => new { r.StudentId, r.SessionId });
    }
}
