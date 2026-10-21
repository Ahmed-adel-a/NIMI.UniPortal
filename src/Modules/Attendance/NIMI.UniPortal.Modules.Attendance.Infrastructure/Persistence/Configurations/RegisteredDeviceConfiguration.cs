using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Attendance.Domain.Devices;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence.Configurations;

internal sealed class RegisteredDeviceConfiguration : IEntityTypeConfiguration<RegisteredDevice>
{
    public void Configure(EntityTypeBuilder<RegisteredDevice> builder)
    {
        builder.ToTable("registered_devices");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.StudentId);
        builder.Property(d => d.Fingerprint).HasMaxLength(200);
        builder.Property(d => d.RegisteredAt);
        builder.Property(d => d.RevokedAt);

        builder.HasIndex(d => d.StudentId)
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ux_registered_devices_student_active");
        builder.HasIndex(d => d.StudentId);
    }
}
