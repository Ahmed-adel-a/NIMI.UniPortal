using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Attendance.Domain.Devices;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence.Configurations;

internal sealed class DeviceChangeRequestConfiguration : IEntityTypeConfiguration<DeviceChangeRequest>
{
    public void Configure(EntityTypeBuilder<DeviceChangeRequest> builder)
    {
        builder.ToTable("device_change_requests", table =>
            table.HasCheckConstraint(
                "ck_device_change_requests_status",
                "status IN ('pending','approved','rejected')"));

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.StudentId);
        builder.Property(r => r.NewFingerprint).HasMaxLength(200);
        builder.Property(r => r.Reason).HasMaxLength(500);
        builder.Property(r => r.Status)
            .HasConversion(new SnakeCaseEnumConverter<DeviceChangeStatus>())
            .HasMaxLength(20);
        builder.Property(r => r.DecidedBy);
        builder.Property(r => r.DecidedAt);
        builder.Property(r => r.CreatedAt);

        builder.HasIndex(r => r.StudentId)
            .IsUnique()
            .HasFilter("status = 'pending'")
            .HasDatabaseName("ux_device_change_requests_student_pending");
        builder.HasIndex(r => new { r.Status, r.CreatedAt });
    }
}
