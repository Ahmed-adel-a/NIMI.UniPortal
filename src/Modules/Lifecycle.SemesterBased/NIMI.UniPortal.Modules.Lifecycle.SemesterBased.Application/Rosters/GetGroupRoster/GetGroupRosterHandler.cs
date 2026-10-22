using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Rosters.GetGroupRoster;

/// <summary>Same contract as the CreditHours roster (the frontend uses one screen): the group's enrolled students.</summary>
internal sealed class GetGroupRosterHandler(IAcademicLifecycle lifecycle, ISchedulingApi scheduling, IUsersApi users)
    : IRequestHandler<GetGroupRosterQuery, PagedResult<RosterStudentDto>>
{
    public async Task<PagedResult<RosterStudentDto>> Handle(GetGroupRosterQuery query, CancellationToken cancellationToken)
    {
        var group = await scheduling.GetGroupAsync(query.GroupId, cancellationToken)
            ?? throw new NotFoundException($"No group with id '{query.GroupId}'.");

        if (!query.IsAdmin)
        {
            if (!query.IsProfessor)
                throw new ForbiddenException("Only the group's professor can see its roster.");

            if (!await scheduling.IsTeachingAsync(query.StaffId, group.SubjectId, group.TermId, group.Id, cancellationToken))
                throw new ForbiddenException("You do not teach this group.");
        }

        var students = new List<UserRef>();
        foreach (var studentId in await lifecycle.GetStudentsInGroupAsync(query.GroupId, cancellationToken))
        {
            if (await users.GetUserAsync(studentId, cancellationToken) is { } student)
                students.Add(student);
        }

        var ordered = students
            .OrderBy(s => s.StudentCode ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(s => s.Id)
            .ToList();
        var page = ordered
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(s => new RosterStudentDto(s.Id, s.FullName, s.StudentCode ?? string.Empty))
            .ToList();

        return new PagedResult<RosterStudentDto>(page, query.Paging.Page, query.Paging.PageSize, ordered.Count);
    }
}
