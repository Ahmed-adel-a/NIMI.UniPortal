using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Review;

internal sealed class ListSubmissionsHandler(IAssignmentsDbContext db, AssignmentAccess access)
    : IRequestHandler<ListSubmissionsQuery, PagedResult<SubmissionDto>>
{
    public async Task<PagedResult<SubmissionDto>> Handle(ListSubmissionsQuery query, CancellationToken cancellationToken)
    {
        var status = SubmissionStatuses.Parse(query.Status);
        var (assignment, _) = await access.LoadForTeacherAsync(query.AssignmentId, query.Caller, cancellationToken);

        var submissions = db.Submissions.AsNoTracking().Where(s => s.AssignmentId == assignment.Id);
        if (status is { } wanted)
            submissions = submissions.Where(s => s.Status == wanted);

        var total = await submissions.CountAsync(cancellationToken);
        var page = await submissions
            .OrderBy(s => s.SubmittedAt)
            .ThenBy(s => s.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SubmissionDto>([.. page.Select(SubmissionDto.From)], query.Paging.Page, query.Paging.PageSize, total);
    }
}
