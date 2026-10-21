using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable;

/// <summary>Shared conflict query for slot create/update (rule R8): load only plausible candidates, then ask the domain.</summary>
internal static class ScheduleSlotConflicts
{
    public static async Task<ScheduleSlot?> FindAsync(
        ISchedulingDbContext db,
        ScheduleSlot candidate,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var staffId = candidate.StaffId;
        var candidates = await db.ScheduleSlots.AsNoTracking()
            .Where(s => !s.IsCancelled)
            .Where(s => s.DayOfWeek == candidate.DayOfWeek)
            .Where(s => s.ValidUntil >= candidate.ValidFrom && s.ValidFrom <= candidate.ValidUntil)
            .Where(s => excludeId == null || s.Id != excludeId)
            .Where(s =>
                s.FacilityId == candidate.FacilityId ||
                s.SubjectGroupId == candidate.SubjectGroupId ||
                (staffId != null && s.StaffId == staffId))
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(candidate.ConflictsWith);
    }

    public static string Describe(ScheduleSlot candidate, ScheduleSlot other)
    {
        var kinds = new List<string>();
        if (candidate.FacilityId == other.FacilityId)
            kinds.Add("facility");
        if (candidate.SubjectGroupId == other.SubjectGroupId)
            kinds.Add("group");
        if (candidate.StaffId is not null && candidate.StaffId == other.StaffId)
            kinds.Add("staff");

        return $"The slot conflicts with slot {other.Id} on: {string.Join(", ", kinds)}.";
    }
}
