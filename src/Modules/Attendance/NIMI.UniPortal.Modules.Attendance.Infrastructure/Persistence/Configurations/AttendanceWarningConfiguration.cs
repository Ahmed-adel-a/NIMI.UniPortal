using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence.Configurations;

internal sealed class AttendanceWarningConfiguration : IEntityTypeConfiguration<AttendanceWarning>
{
    public void Configure(EntityTypeBuilder<AttendanceWarning> builder)
    {
        builder.ToTable("attendance_warnings");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();
        builder.Property(w => w.StudentId);
        builder.Property(w => w.SubjectGroupId);
        builder.Property(w => w.WarnedAt);

        builder.HasIndex(w => new { w.StudentId, w.SubjectGroupId })
            .IsUnique()
            .HasDatabaseName("ux_attendance_warnings_student_group");
        builder.HasIndex(w => w.SubjectGroupId);
    }
}
