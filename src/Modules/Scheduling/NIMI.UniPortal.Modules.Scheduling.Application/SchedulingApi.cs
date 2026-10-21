using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

namespace NIMI.UniPortal.Modules.Scheduling.Application;

/// <summary>The Scheduling module's public contract: read-only, DTOs only, never entities or IQueryable (P3.7).</summary>
internal sealed class SchedulingApi(ISchedulingDbContext db) : ISchedulingApi
{
    public async Task<TermRef?> GetCurrentTermAsync(CancellationToken cancellationToken = default)
    {
        var term = await db.Terms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Status == TermStatus.Active, cancellationToken);
        return term is null ? null : ToTermRef(term);
    }

    public async Task<TermRef?> GetTermAsync(Guid termId, CancellationToken cancellationToken = default)
    {
        var term = await db.Terms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == termId, cancellationToken);
        return term is null ? null : ToTermRef(term);
    }

    public async Task<SubjectRef?> GetSubjectAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        var subject = await db.Subjects.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subjectId, cancellationToken);
        return subject is null ? null : ToSubjectRef(subject);
    }

    public async Task<GroupRef?> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await db.SubjectGroups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        return group is null ? null : ToGroupRef(group);
    }

    public async Task<IReadOnlyList<OfferingRef>> GetOfferingsAsync(
        Guid termId, CancellationToken cancellationToken = default)
    {
        var groups = await db.SubjectGroups.AsNoTracking()
            .Include(g => g.Subject)
            .Where(g => g.TermId == termId && g.IsActive)
            .OrderBy(g => g.Subject.Code)
            .ThenBy(g => g.GroupNumber)
            .ToListAsync(cancellationToken);

        if (groups.Count == 0)
            return [];

        var groupIds = groups.Select(g => g.Id).ToList();
        var slots = await db.ScheduleSlots.AsNoTracking()
            .Include(s => s.Facility)
            .Where(s => groupIds.Contains(s.SubjectGroupId) && !s.IsCancelled)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var slotsByGroup = slots
            .GroupBy(s => s.SubjectGroupId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SlotRef>)g.Select(ToSlotRef).ToList());

        return groups
            .Select(g => new OfferingRef(
                ToGroupRef(g),
                ToSubjectRef(g.Subject),
                slotsByGroup.TryGetValue(g.Id, out var groupSlots) ? groupSlots : []))
            .ToList();
    }

    public async Task<IReadOnlyList<SlotRef>> GetSlotsForGroupsAsync(
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0)
            return [];

        var slots = await db.ScheduleSlots.AsNoTracking()
            .Include(s => s.Facility)
            .Where(s => groupIds.Contains(s.SubjectGroupId))
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        return slots.Select(ToSlotRef).ToList();
    }

    public async Task<SlotRef?> GetSlotAsync(Guid slotId, CancellationToken cancellationToken = default)
    {
        var slot = await db.ScheduleSlots.AsNoTracking()
            .Include(s => s.Facility)
            .FirstOrDefaultAsync(s => s.Id == slotId, cancellationToken);
        return slot is null ? null : ToSlotRef(slot);
    }

    public Task<bool> IsTeachingAsync(
        Guid staffId,
        Guid subjectId,
        Guid termId,
        Guid? groupId,
        CancellationToken cancellationToken = default) =>
        db.TeachingAssignments.AsNoTracking()
            .AnyAsync(
                a => a.IsActive &&
                     a.StaffId == staffId &&
                     a.SubjectId == subjectId &&
                     a.TermId == termId &&
                     (a.SubjectGroupId == null || a.SubjectGroupId == groupId),
                cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetGroupIdsTaughtByAsync(
        Guid staffId, Guid termId, CancellationToken cancellationToken = default)
    {
        var assignments = await db.TeachingAssignments.AsNoTracking()
            .Where(a => a.IsActive && a.StaffId == staffId && a.TermId == termId)
            .Select(a => new { a.SubjectId, a.SubjectGroupId })
            .ToListAsync(cancellationToken);

        if (assignments.Count == 0)
            return [];

        var namedGroups = assignments
            .Where(a => a.SubjectGroupId is not null)
            .Select(a => a.SubjectGroupId!.Value)
            .ToHashSet();
        var wholeSubjects = assignments
            .Where(a => a.SubjectGroupId is null)
            .Select(a => a.SubjectId)
            .ToHashSet();

        var groups = await db.SubjectGroups.AsNoTracking()
            .Where(g => g.TermId == termId && g.IsActive &&
                        (namedGroups.Contains(g.Id) || wholeSubjects.Contains(g.SubjectId)))
            .OrderBy(g => g.SubjectId)
            .ThenBy(g => g.GroupNumber)
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        return groups;
    }

    public async Task<IReadOnlyList<OccurrenceRef>> GetOccurrencesAsync(
        IReadOnlyCollection<Guid> groupIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0)
            return [];

        var slots = await db.ScheduleSlots.AsNoTracking()
            .Include(s => s.SubjectGroup)
            .ThenInclude(g => g.Subject)
            .Include(s => s.Facility)
            .Where(s => groupIds.Contains(s.SubjectGroupId))
            .Where(s => !s.IsCancelled && s.ValidFrom <= to && s.ValidUntil >= from)
            .ToListAsync(cancellationToken);

        return slots
            .SelectMany(slot => slot.OccurrencesBetween(from, to)
                .Select(date => new OccurrenceRef(
                    date,
                    slot.DayOfWeek,
                    slot.StartTime,
                    slot.EndTime,
                    slot.Id,
                    slot.SubjectGroupId,
                    slot.SubjectGroup.GroupNumber,
                    slot.SubjectGroup.SubjectId,
                    slot.SubjectGroup.Subject.Code,
                    slot.SubjectGroup.Subject.Name,
                    slot.SubjectGroup.Subject.NameAr,
                    slot.Facility.Name,
                    slot.StaffId)))
            .OrderBy(o => o.Date)
            .ThenBy(o => o.StartTime)
            .ThenBy(o => o.SlotId)
            .ToList();
    }

    public async Task<IReadOnlyList<Guid>> FindTimeConflictsAsync(
        Guid candidateGroupId,
        IReadOnlyCollection<Guid> otherGroupIds,
        CancellationToken cancellationToken = default)
    {
        if (otherGroupIds.Count == 0)
            return [];

        var ids = otherGroupIds.Distinct().ToList();
        var slots = await db.ScheduleSlots.AsNoTracking()
            .Where(s => !s.IsCancelled)
            .Where(s => s.SubjectGroupId == candidateGroupId || ids.Contains(s.SubjectGroupId))
            .ToListAsync(cancellationToken);

        var byGroup = slots
            .GroupBy(s => s.SubjectGroupId)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (!byGroup.TryGetValue(candidateGroupId, out var candidateSlots))
            return [];

        return ids
            .Where(id => byGroup.TryGetValue(id, out var others) &&
                         candidateSlots.Any(candidate => others.Any(candidate.OverlapsInTime)))
            .ToList();
    }

    public async Task<IReadOnlyList<SubjectRef>> GetGraduationRequirementSubjectsAsync(
        CancellationToken cancellationToken = default)
    {
        var subjects = await db.Subjects.AsNoTracking()
            .Where(s => s.IsGraduationRequirement && s.IsActive)
            .OrderBy(s => s.Code)
            .ToListAsync(cancellationToken);
        return subjects.Select(ToSubjectRef).ToList();
    }

    private static TermRef ToTermRef(Term term) =>
        new(
            term.Id,
            term.AcademicYear,
            term.Number.ToString().ToLowerInvariant(),
            term.StartsOn,
            term.EndsOn,
            term.WeekCount,
            term.Status.ToString().ToLowerInvariant());

    private static SubjectRef ToSubjectRef(Subject subject) =>
        new(
            subject.Id,
            subject.DepartmentId,
            subject.Code,
            subject.Name,
            subject.NameAr,
            subject.CreditHours,
            subject.Category == SubjectCategory.MilitaryEducation ? "military_education" : "regular",
            subject.IsGraduationRequirement,
            subject.IsActive);

    private static GroupRef ToGroupRef(SubjectGroup group) =>
        new(
            group.Id,
            group.SubjectId,
            group.TermId,
            group.GroupNumber,
            group.TargetDepartmentId,
            group.Capacity,
            group.IsActive);

    private static SlotRef ToSlotRef(ScheduleSlot slot) =>
        new(
            slot.Id,
            slot.SubjectGroupId,
            slot.FacilityId,
            slot.Facility.Name,
            slot.StaffId,
            slot.DayOfWeek,
            slot.StartTime,
            slot.EndTime,
            slot.Recurrence.ToString().ToLowerInvariant(),
            slot.ValidFrom,
            slot.ValidUntil,
            slot.IsCancelled);
}
