using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Hosting.Lifecycle;

/// <summary>
/// Decision LIFECYCLEDEPT: the student-facing lifecycle reads both academic systems share (schedule, rosters,
/// progress), answered through the lifecycle router so the frontend never needs to know which system a student is in.
/// </summary>
internal sealed class StudentLifecycleReads(
    IAcademicLifecycle lifecycle, ISchedulingApi scheduling, IUsersApi users, IPlatformSettingsApi platform)
{
    public async Task<IReadOnlyList<StudentOccurrenceDto>> ScheduleAsync(
        Guid studentId, bool isStudent, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!isStudent)
            throw new ForbiddenException("Only students have a student schedule.");
        if (from is not { } start)
            throw ValidationErrors.For("from", "Required.");
        if (to is not { } end)
            throw ValidationErrors.For("to", "Required.");
        if (end < start)
            throw ValidationErrors.For("to", "Must be on or after 'from'.");
        if (end.DayNumber - start.DayNumber > 30)   // same rule as the teaching schedule: at most 31 days
            throw ValidationErrors.For("to", "The range is at most 31 calendar days.");

        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        if (term is null)
            return [];
        var groupIds = await lifecycle.GetGroupsForStudentAsync(studentId, term.Id, cancellationToken);
        if (groupIds.Count == 0)
            return [];

        var occurrences = await scheduling.GetOccurrencesAsync(groupIds, start, end, cancellationToken);
        return
        [
            .. occurrences
                .Select(o => new StudentOccurrenceDto(
                    o.Date, o.DayOfWeek, o.StartTime, o.EndTime, o.SlotId, o.SubjectCode, o.SubjectName, o.SubjectNameAr,
                    o.GroupNumber, o.FacilityName))
                .OrderBy(o => o.Date).ThenBy(o => o.StartTime).ThenBy(o => o.ScheduleId),
        ];
    }

    /// <summary>Decision FRONTEND: the student's subjects and groups this term, by code, from their own academic system.</summary>
    public async Task<IReadOnlyList<StudentCourseDto>> CoursesAsync(Guid studentId, bool isStudent, CancellationToken cancellationToken)
    {
        if (!isStudent)
            throw new ForbiddenException("Only students have courses of their own.");

        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        if (term is null)
            return [];

        var courses = new List<StudentCourseDto>();
        foreach (var groupId in await lifecycle.GetGroupsForStudentAsync(studentId, term.Id, cancellationToken))
        {
            if (await scheduling.GetGroupAsync(groupId, cancellationToken) is not { } group
                || await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken) is not { } subject)
                continue;
            courses.Add(new StudentCourseDto(
                subject.Id, subject.Code, subject.Name, subject.NameAr, subject.CreditHours, group.Id, group.GroupNumber));
        }

        return [.. courses.OrderBy(c => c.SubjectCode, StringComparer.Ordinal)];
    }

    /// <summary>A group's members from both systems; the group's teaching staff or Student Affairs only.</summary>
    public async Task<PagedResult<RosterStudentDto>> RosterAsync(
        Guid groupId, Guid staffId, bool isProfessor, bool isAdmin, PageRequest paging, CancellationToken cancellationToken)
    {
        var group = await scheduling.GetGroupAsync(groupId, cancellationToken)
            ?? throw new NotFoundException($"No group with id '{groupId}'.");
        if (!isAdmin)
        {
            if (!isProfessor)
                throw new ForbiddenException("Only the group's professor can see its roster.");
            if (!await scheduling.IsTeachingAsync(staffId, group.SubjectId, group.TermId, group.Id, cancellationToken))
                throw new ForbiddenException("You do not teach this group.");
        }

        var students = new List<UserRef>();
        foreach (var studentId in await lifecycle.GetStudentsInGroupAsync(groupId, cancellationToken))
        {
            if (await users.GetUserAsync(studentId, cancellationToken) is { } student)
                students.Add(student);
        }

        var ordered = students.OrderBy(s => s.StudentCode ?? string.Empty, StringComparer.Ordinal).ThenBy(s => s.Id).ToList();
        return new PagedResult<RosterStudentDto>(
            [.. ordered.Skip(paging.Skip).Take(paging.PageSize).Select(s => new RosterStudentDto(s.Id, s.FullName, s.StudentCode ?? string.Empty))],
            paging.Page, paging.PageSize, ordered.Count);
    }

    public async Task<StudentProgressDto> ProgressAsync(Guid studentId, bool isStudent, CancellationToken cancellationToken)
    {
        if (!isStudent)
            throw new ForbiddenException("Only students have progress.");

        var progress = await lifecycle.GetProgressAsync(studentId, cancellationToken);
        var placement = await users.GetStudentPlacementAsync(studentId, cancellationToken);
        var system = await platform.GetAcademicSystemAsync(placement?.DepartmentId, cancellationToken);

        // The seam reports ids only; codes and names come from Scheduling, never copied.
        var outstanding = new List<OutstandingRequirementDto>(progress.OutstandingRequiredSubjectIds.Count);
        foreach (var subjectId in progress.OutstandingRequiredSubjectIds)
        {
            if (await scheduling.GetSubjectAsync(subjectId, cancellationToken) is { } subject)
                outstanding.Add(new OutstandingRequirementDto(subject.Id, subject.Code, subject.Name, subject.NameAr));
        }

        return new StudentProgressDto(
            progress.StudentId, system, progress.CreditsEarned, progress.CreditsInProgress, progress.CumulativeGpa, outstanding);
    }
}
