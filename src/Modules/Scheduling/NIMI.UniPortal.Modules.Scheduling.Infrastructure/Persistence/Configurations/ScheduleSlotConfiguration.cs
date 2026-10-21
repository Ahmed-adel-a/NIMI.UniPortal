using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

namespace NIMI.UniPortal.Modules.Scheduling.Infrastructure.Persistence.Configurations;

internal sealed class ScheduleSlotConfiguration : IEntityTypeConfiguration<ScheduleSlot>
{
    public void Configure(EntityTypeBuilder<ScheduleSlot> builder)
    {
        builder.ToTable("schedules", table =>
        {
            table.HasCheckConstraint("ck_schedules_day", "day_of_week BETWEEN 0 AND 6");
            table.HasCheckConstraint("ck_schedules_time", "end_time > start_time");
            table.HasCheckConstraint("ck_schedules_recurrence", "recurrence IN ('weekly','biweekly','once')");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.SubjectGroupId);
        builder.Property(s => s.FacilityId);
        builder.Property(s => s.StaffId);
        builder.Property(s => s.DayOfWeek);
        builder.Property(s => s.StartTime);
        builder.Property(s => s.EndTime);
        builder.Property(s => s.Recurrence)
            .HasConversion(new SnakeCaseEnumConverter<Recurrence>())
            .HasMaxLength(10);
        builder.Property(s => s.ValidFrom);
        builder.Property(s => s.ValidUntil);
        builder.Property(s => s.IsCancelled).HasDefaultValue(false);
        builder.Property(s => s.CreatedBy);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt);

        builder.HasOne(s => s.SubjectGroup)
            .WithMany()
            .HasForeignKey(s => s.SubjectGroupId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Facility)
            .WithMany()
            .HasForeignKey(s => s.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.FacilityId, s.DayOfWeek });
        builder.HasIndex(s => s.SubjectGroupId);
        builder.HasIndex(s => new { s.StaffId, s.DayOfWeek });
    }
}
