using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Rosters.GetGroupRoster;

internal sealed class GetGroupRosterHandler(
    ICreditHoursDbContext db, ISchedulingApi scheduling, IUsersApi users)
    : IRequestHandler<GetGroupRosterQuery, PagedResult<RosterStudentDto>>
{
    public async Task<PagedResult<RosterStudentDto>> Handle(
        GetGroupRosterQuery query, CancellationToken cancellationToken)
    {
        var group = await scheduling.GetGroupAsync(query.GroupId, cancellationToken)
            ?? throw new NotFoundException($"No group with id '{query.GroupId}'.");

        if (!query.IsAdmin)
        {
            if (!query.IsProfessor)
                throw new ForbiddenException("Only the group's professor can see its roster.");

            if (!await scheduling.IsTeachingAsync(
                    query.StaffId, group.SubjectId, group.TermId, group.Id, cancellationToken))
                throw new ForbiddenException("You do not teach this group.");
        }

        // R11: only confirmed members — a pending military-education row or a dropped one is not a roster member.
        var studentIds = await db.CourseRegistrations.AsNoTracking()
            .Where(r => r.SubjectGroupId == query.GroupId &&
                        (r.Status == RegistrationStatus.Registered ||
                         r.Status == RegistrationStatus.Completed ||
                         r.Status == RegistrationStatus.Failed))
            .Select(r => r.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var students = new List<UserRef>(studentIds.Count);
        foreach (var studentId in studentIds)
        {
            var student = await users.GetUserAsync(studentId, cancellationToken);
            if (student is not null)
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

        return new PagedResult<RosterStudentDto>(
            page, query.Paging.Page, query.Paging.PageSize, ordered.Count);
    }
}
