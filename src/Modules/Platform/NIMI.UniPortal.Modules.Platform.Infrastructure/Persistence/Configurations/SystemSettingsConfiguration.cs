using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Platform.Domain.Settings;

namespace NIMI.UniPortal.Modules.Platform.Infrastructure.Persistence.Configurations;

internal sealed class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    /// <summary>Fixed so the seed (and its migration) never changes between builds.</summary>
    private static readonly DateTimeOffset SeededAt = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("system_settings", table =>
        {
            // Exactly one row per deployment — enforced by the database, not only by code.
            table.HasCheckConstraint("ck_system_settings_singleton", $"id = '{SystemSettings.SingletonId}'");
            table.HasCheckConstraint("ck_system_settings_week_start_day", "week_start_day BETWEEN 0 AND 6");
            table.HasCheckConstraint("ck_system_settings_attendance_threshold", "attendance_threshold BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_system_settings_late_grace_minutes", "late_grace_minutes BETWEEN 0 AND 180");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.UniversityName).HasMaxLength(255);
        builder.Property(s => s.UniversityNameAr).HasMaxLength(255);
        builder.Property(s => s.Domain).HasMaxLength(255);
        builder.Property(s => s.LogoUrl).HasMaxLength(500);
        builder.Property(s => s.PrimaryColor).HasMaxLength(7);
        builder.Property(s => s.SecondaryColor).HasMaxLength(7);
        builder.Property(s => s.TimeZone).HasColumnName("timezone").HasMaxLength(64);
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

        builder.HasData(SystemSettings.CreateDefault(SeededAt));
    }
}
