using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.GetTeachingSchedule;

internal sealed class GetTeachingScheduleHandler(ISchedulingDbContext db)
    : IRequestHandler<GetTeachingScheduleQuery, IReadOnlyList<TeachingOccurrenceDto>>
{
    public async Task<IReadOnlyList<TeachingOccurrenceDto>> Handle(
        GetTeachingScheduleQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsProfessor)
            throw new ForbiddenException("Only professors have a teaching schedule.");

        if (query.From is not { } from)
            throw ValidationErrors.For(nameof(query.From), "Required.");

        if (query.To is not { } to)
            throw ValidationErrors.For(nameof(query.To), "Required.");

        if (to < from)
            throw ValidationErrors.For(nameof(query.To), "Must be on or after 'from'.");

        // R10b: at most 31 calendar days inclusive.
        if (to.DayNumber - from.DayNumber > 30)
            throw ValidationErrors.For(nameof(query.To), "The range is at most 31 calendar days.");

        // A slot belongs to the caller when it names them, or when they have an active assignment for the slot's
        // subject + term that covers the whole subject or exactly this group (rule R10).
        var slots = await db.ScheduleSlots.AsNoTracking()
            .Include(s => s.SubjectGroup)
            .ThenInclude(g => g.Subject)
            .Include(s => s.Facility)
            .Where(s => !s.IsCancelled)
            .Where(s => s.ValidFrom <= to && s.ValidUntil >= from)
            .Where(s =>
                s.StaffId == query.StaffId ||
                db.TeachingAssignments.Any(a =>
                    a.IsActive &&
                    a.StaffId == query.StaffId &&
                    a.SubjectId == s.SubjectGroup.SubjectId &&
                    a.TermId == s.SubjectGroup.TermId &&
                    (a.SubjectGroupId == null || a.SubjectGroupId == s.SubjectGroupId)))
            .ToListAsync(cancellationToken);

        return slots
            .SelectMany(slot => slot.OccurrencesBetween(from, to)
                .Select(date => new TeachingOccurrenceDto(
                    date,
                    slot.DayOfWeek,
                    slot.StartTime,
                    slot.EndTime,
                    slot.Id,
                    slot.SubjectGroup.Subject.Code,
                    slot.SubjectGroup.Subject.Name,
                    slot.SubjectGroup.Subject.NameAr,
                    slot.SubjectGroup.GroupNumber,
                    slot.Facility.Name)))
            .OrderBy(o => o.Date)
            .ThenBy(o => o.StartTime)
            .ThenBy(o => o.ScheduleId)
            .ToList();
    }
}
