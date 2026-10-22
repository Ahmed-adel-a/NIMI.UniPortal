using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Schedule.GetStudentSchedule;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Schedule.GetStudentSchedule;

internal sealed class GetStudentScheduleHandler(IAcademicLifecycle lifecycle, ISchedulingApi scheduling)
    : IRequestHandler<GetStudentScheduleQuery, IReadOnlyList<StudentOccurrenceDto>>
{
    public async Task<IReadOnlyList<StudentOccurrenceDto>> Handle(
        GetStudentScheduleQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsStudent)
            throw new ForbiddenException("Only students have a student schedule.");

        if (query.From is not { } from)
            throw ValidationErrors.For(nameof(query.From), "Required.");

        if (query.To is not { } to)
            throw ValidationErrors.For(nameof(query.To), "Required.");

        if (to < from)
            throw ValidationErrors.For(nameof(query.To), "Must be on or after 'from'.");

        // Same rule as the teaching schedule: at most 31 calendar days inclusive.
        if (to.DayNumber - from.DayNumber > 30)
            throw ValidationErrors.For(nameof(query.To), "The range is at most 31 calendar days.");

        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        if (term is null)
            return [];

        // R11: only confirmed memberships; dropped/pending rows never appear on the schedule.
        var groupIds = await lifecycle.GetGroupsForStudentAsync(query.StudentId, term.Id, cancellationToken);
        if (groupIds.Count == 0)
            return [];

        var occurrences = await scheduling.GetOccurrencesAsync(groupIds, from, to, cancellationToken);

        return occurrences
            .Select(o => new StudentOccurrenceDto(
                o.Date,
                o.DayOfWeek,
                o.StartTime,
                o.EndTime,
                o.SlotId,
                o.SubjectCode,
                o.SubjectName,
                o.SubjectNameAr,
                o.GroupNumber,
                o.FacilityName))
            .OrderBy(o => o.Date)
            .ThenBy(o => o.StartTime)
            .ThenBy(o => o.ScheduleId)
            .ToList();
    }
}
