using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.Modules.Assignments.Domain;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Assignments.Application.Browse;

/// <summary>
/// P8.2 — with a subject: its teaching staff and Student Affairs see everything, a registered student sees the
/// subject-wide items and their own group's (R2), anyone else is refused. Without a subject: a student's registered
/// subjects, a professor's taught subjects, or everything for Student Affairs — always the active term.
/// </summary>
internal sealed class ListAssignmentsHandler(
    IAssignmentsDbContext db,
    CourseAccess course,
    ISchedulingApi scheduling,
    AssignmentPresenter presenter,
    TimeProvider clock) : IRequestHandler<ListAssignmentsQuery, PagedResult<AssignmentDto>>
{
    public async Task<PagedResult<AssignmentDto>> Handle(ListAssignmentsQuery query, CancellationToken cancellationToken)
    {
        var open = ParseStatus(query.Status);
        var term = await scheduling.GetCurrentTermAsync(cancellationToken);
        if (term is null)
            return Empty(query.Paging);

        var assignments = db.Assignments.AsNoTracking().Where(a => a.TermId == term.Id && !a.IsDeleted);
        bool staffView;

        if (query.SubjectId is { } subjectId)
        {
            _ = await scheduling.GetSubjectAsync(subjectId, cancellationToken) ?? throw new NotFoundException("Subject not found.");
            assignments = assignments.Where(a => a.SubjectId == subjectId);

            staffView = query.Caller.IsAdmin ||
                        (query.Caller.IsProfessor && await course.TeachesSubjectAsync(query.Caller.UserId, subjectId, term.Id, cancellationToken));
            if (!staffView)
            {
                var groups = query.Caller.IsStudent
                    ? await course.StudentGroupsAsync(query.Caller.UserId, subjectId, term.Id, cancellationToken)
                    : [];
                if (groups.Count == 0)
                    throw new ForbiddenException("You are not registered in or teaching this subject.");
                assignments = assignments.Where(a => a.SubjectGroupId == null || groups.Contains(a.SubjectGroupId.Value));
            }
        }
        else if (query.Caller.IsAdmin)
        {
            staffView = true;
        }
        else if (query.Caller.IsProfessor)
        {
            staffView = true;
            var subjects = await course.TaughtSubjectsAsync(query.Caller.UserId, term.Id, cancellationToken);
            assignments = assignments.Where(a => subjects.Contains(a.SubjectId));
        }
        else if (query.Caller.IsStudent)
        {
            staffView = false;
            var groups = await course.StudentGroupsInTermAsync(query.Caller.UserId, term.Id, cancellationToken);
            var subjects = groups.Select(g => g.SubjectId).Distinct().ToList();
            var groupIds = groups.Select(g => g.Id).ToList();
            assignments = assignments.Where(a =>
                subjects.Contains(a.SubjectId) && (a.SubjectGroupId == null || groupIds.Contains(a.SubjectGroupId.Value)));
        }
        else
        {
            throw new ForbiddenException("Not available for this account.");
        }

        var now = clock.GetUtcNow();
        if (open is true)
            assignments = assignments.Where(a => a.DueAt > now);
        else if (open is false)
            assignments = assignments.Where(a => a.DueAt <= now);

        var total = await assignments.CountAsync(cancellationToken);
        var page = await assignments
            .OrderBy(a => a.DueAt)
            .ThenBy(a => a.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AssignmentDto>(
            await presenter.PresentAsync(page, query.Caller, staffView, cancellationToken),
            query.Paging.Page,
            query.Paging.PageSize,
            total);
    }

    private static bool? ParseStatus(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            null or "" => null,
            "open" => true,
            "closed" => false,
            _ => throw ValidationErrors.For("status", "Status must be open or closed."),
        };

    private static PagedResult<AssignmentDto> Empty(PageRequest paging) =>
        new([], paging.Page, paging.PageSize, 0);
}
