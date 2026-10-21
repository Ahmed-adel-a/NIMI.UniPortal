using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Mine;

internal sealed class MySubmissionsHandler(IAssignmentsDbContext db, ISchedulingApi scheduling)
    : IRequestHandler<MySubmissionsQuery, PagedResult<MySubmissionDto>>
{
    public async Task<PagedResult<MySubmissionDto>> Handle(MySubmissionsQuery query, CancellationToken cancellationToken)
    {
        if (!query.Caller.IsStudent)
            throw new ForbiddenException("Only students have submissions.");

        var status = SubmissionStatuses.Parse(query.Status);
        var rows = db.Submissions.AsNoTracking()
            .Where(s => s.StudentId == query.Caller.UserId)
            .Join(db.Assignments.AsNoTracking(), s => s.AssignmentId, a => a.Id, (s, a) => new { Submission = s, Assignment = a });
        if (status is { } wanted)
            rows = rows.Where(x => x.Submission.Status == wanted);

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .OrderByDescending(x => x.Submission.SubmittedAt)
            .ThenByDescending(x => x.Submission.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var codes = new Dictionary<Guid, string>();
        foreach (var subjectId in page.Select(x => x.Assignment.SubjectId).Distinct())
            codes[subjectId] = (await scheduling.GetSubjectAsync(subjectId, cancellationToken))?.Code ?? string.Empty;

        return new PagedResult<MySubmissionDto>(
            [.. page.Select(x => new MySubmissionDto(
                x.Submission.Id,
                x.Assignment.Id,
                x.Assignment.Title,
                codes[x.Assignment.SubjectId],
                x.Submission.ReceiptNumber,
                x.Submission.SubmittedAt,
                x.Submission.Status,
                x.Submission.Grade,
                x.Assignment.MaxGrade,
                x.Submission.Feedback))],
            query.Paging.Page,
            query.Paging.PageSize,
            total);
    }
}
