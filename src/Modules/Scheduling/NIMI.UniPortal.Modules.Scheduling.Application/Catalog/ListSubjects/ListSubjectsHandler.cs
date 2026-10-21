using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListSubjects;

internal sealed class ListSubjectsHandler(ISchedulingDbContext db)
    : IRequestHandler<ListSubjectsQuery, PagedResult<SubjectDto>>
{
    public async Task<PagedResult<SubjectDto>> Handle(ListSubjectsQuery query, CancellationToken cancellationToken)
    {
        var subjects = db.Subjects.AsNoTracking()
            .Where(s => query.DepartmentId == null || s.DepartmentId == query.DepartmentId)
            .Where(s => query.IncludeInactive || s.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            subjects = subjects.Where(s =>
                s.Code.ToLower().Contains(search) ||
                s.Name.ToLower().Contains(search) ||
                s.NameAr.ToLower().Contains(search));
        }

        var total = await subjects.CountAsync(cancellationToken);
        var items = await subjects
            .OrderBy(s => s.Code)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(s => new SubjectDto(
                s.Id, s.DepartmentId, s.Code, s.Name, s.NameAr, s.Description,
                s.CreditHours, s.Category, s.IsGraduationRequirement, s.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<SubjectDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
