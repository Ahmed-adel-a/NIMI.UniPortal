using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;

namespace NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence.Configurations;

/// <summary>platform.faculty_settings — at most one override row per faculty (P1.4).</summary>
internal sealed class FacultySettingsConfiguration : IEntityTypeConfiguration<FacultySettings>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public void Configure(EntityTypeBuilder<FacultySettings> builder)
    {
        builder.ToTable("faculty_settings", table =>
        {
            table.HasCheckConstraint("ck_faculty_settings_week_start_day", "week_start_day BETWEEN 0 AND 6");
            table.HasCheckConstraint("ck_faculty_settings_attendance_threshold", "attendance_threshold BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_faculty_settings_late_grace_minutes", "late_grace_minutes BETWEEN 0 AND 180");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        // Same optimistic-concurrency stamp as system_settings (PostgreSQL's xmin) — see SystemSettingsConfiguration.
        builder.Property<uint>(SettingsVersion.PropertyName)
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid");

        builder.Property(s => s.FacultyId).IsRequired();
        builder.HasIndex(s => s.FacultyId).IsUnique();   // cross-module id: index, no FK

        builder.Property(s => s.AttendanceThreshold).HasPrecision(5, 2);
        builder.Property(s => s.StudentCodeFormat).HasMaxLength(100);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");

        builder.Property(s => s.GradingScale)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<GradeBand[]>(v, Json)!,
                new ValueComparer<IReadOnlyList<GradeBand>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (hash, band) => HashCode.Combine(hash, band)),
                    v => v.ToArray()));

        builder.Property(s => s.MilitaryEducationPolicy)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<MilitaryEducationPolicy>(v, Json)!);
    }
}
