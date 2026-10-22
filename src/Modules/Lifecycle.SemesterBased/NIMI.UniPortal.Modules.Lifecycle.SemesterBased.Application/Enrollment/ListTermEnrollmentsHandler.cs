using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;

internal sealed class ListTermEnrollmentsHandler(ISemesterDbContext db)
    : IRequestHandler<ListTermEnrollmentsQuery, PagedResult<TermEnrollmentDto>>
{
    public async Task<PagedResult<TermEnrollmentDto>> Handle(ListTermEnrollmentsQuery query, CancellationToken cancellationToken)
    {
        var enrollments = db.TermEnrollments.AsNoTracking();
        if (query.TermId is { } termId)
            enrollments = enrollments.Where(e => e.TermId == termId);
        if (query.StudentId is { } studentId)
            enrollments = enrollments.Where(e => e.StudentId == studentId);
        if (query.SubjectGroupId is { } groupId)
            enrollments = enrollments.Where(e => e.SubjectGroupId == groupId);

        var total = await enrollments.CountAsync(cancellationToken);
        var page = await enrollments
            .OrderBy(e => e.StudentId).ThenBy(e => e.Level).ThenBy(e => e.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TermEnrollmentDto>(
            [.. page.Select(TermEnrollmentDto.From)], query.Paging.Page, query.Paging.PageSize, total);
    }
}
