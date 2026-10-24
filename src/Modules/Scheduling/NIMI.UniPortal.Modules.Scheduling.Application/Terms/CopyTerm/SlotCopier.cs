using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Application.Timetable;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CopyTerm;

/// <summary>
/// Carries a term's weekly and biweekly slots over to a new term, applying the same rules as creating a slot by hand
/// (active hall and lecturer, working day, no double-booking) but reporting a failure instead of throwing. One-off and
/// cancelled slots stay behind; each slot keeps its distance from the term's start.
/// </summary>
internal sealed class SlotCopier(ISchedulingDbContext db, IUsersApi users, IPlatformSettingsApi platform)
{
    public async Task<int> CopyAsync(
        Term source,
        Term term,
        IReadOnlyDictionary<Guid, CopiedGroup> groups,
        Guid createdBy,
        DateTimeOffset now,
        List<SkippedItemDto> skipped,
        CancellationToken cancellationToken)
    {
        var sourceGroupIds = groups.Keys.ToList();
        var sourceSlots = (await db.ScheduleSlots.AsNoTracking()
                .Where(s => !s.IsCancelled && s.Recurrence != Recurrence.Once && sourceGroupIds.Contains(s.SubjectGroupId))
                .ToListAsync(cancellationToken))
            .OrderBy(s => groups[s.SubjectGroupId].SubjectCode)
            .ThenBy(s => groups[s.SubjectGroupId].Group.GroupNumber)
            .ThenBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToList();
        if (sourceSlots.Count == 0)
            return 0;

        var facilityIds = sourceSlots.Select(s => s.FacilityId).Distinct().ToList();
        var facilities = await db.Facilities.AsNoTracking()
            .Where(f => facilityIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, cancellationToken);
        var staff = (await users.GetUsersAsync(
                sourceSlots.Where(s => s.StaffId is not null).Select(s => s.StaffId!.Value).Distinct().ToList(), cancellationToken))
            .ToDictionary(u => u.Id);
        var workingDays = new Dictionary<Guid, int[]>();

        var accepted = new List<ScheduleSlot>();
        foreach (var sourceSlot in sourceSlots)
        {
            var group = groups[sourceSlot.SubjectGroupId];
            var validFrom = term.ShiftFrom(source, sourceSlot.ValidFrom);
            var validUntil = term.ShiftFrom(source, sourceSlot.ValidUntil) ?? term.EndsOn;

            facilities.TryGetValue(sourceSlot.FacilityId, out var facility);
            SkipReason? reason = null;
            if (facility is null || !facility.IsActive)
                reason = SkipReason.FacilityInactive;
            else if (sourceSlot.StaffId is { } staffId && !IsActiveTeacher(staff, staffId))
                reason = SkipReason.StaffInactive;
            else if (!(await WorkingDaysAsync(group.DepartmentId, workingDays, cancellationToken)).Contains(sourceSlot.DayOfWeek))
                reason = SkipReason.NotAWorkingDay;
            else if (validFrom is null || validUntil < validFrom)
                reason = SkipReason.OutsideTerm;

            ScheduleSlot? slot = null;
            if (reason is null)
            {
                slot = ScheduleSlot.Create(
                    group.Group, facility!, sourceSlot.StaffId, sourceSlot.DayOfWeek, sourceSlot.StartTime, sourceSlot.EndTime,
                    sourceSlot.Recurrence, validFrom!.Value, validUntil, term, createdBy, now);

                var clash = accepted.Any(slot.ConflictsWith)
                    || await ScheduleSlotConflicts.FindAsync(db, slot, excludeId: null, cancellationToken) is not null;
                if (clash)
                    reason = SkipReason.Conflict;
            }

            if (reason is { } why)
            {
                skipped.Add(new SkippedItemDto(SkippedItemKind.Slot, group.SubjectCode, group.Group.GroupNumber, why));
                continue;
            }

            db.ScheduleSlots.Add(slot!);
            accepted.Add(slot!);
        }

        return accepted.Count;
    }

    private static bool IsActiveTeacher(Dictionary<Guid, UserRef> staff, Guid staffId) =>
        staff.TryGetValue(staffId, out var member) && member.IsActive && PortalRoles.CanTeach(member.Role);

    /// <summary>The faculty's working days (by department); a subject whose department is missing is not held back.</summary>
    private async Task<int[]> WorkingDaysAsync(Guid departmentId, Dictionary<Guid, int[]> cache, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(departmentId, out var cached))
            return cached;

        var department = await users.GetDepartmentAsync(departmentId, cancellationToken);
        var days = department is null
            ? Enumerable.Range(0, 7).ToArray()
            : (await platform.GetEffectiveSettingsAsync(department.FacultyId, cancellationToken)).WorkingDays;
        return cache[departmentId] = days;
    }
}
