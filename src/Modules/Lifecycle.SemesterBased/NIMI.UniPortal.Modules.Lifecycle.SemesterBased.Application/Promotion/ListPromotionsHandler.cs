using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

internal sealed class ListPromotionsHandler(ISemesterDbContext db) : IRequestHandler<ListPromotionsQuery, PagedResult<PromotionDto>>
{
    public async Task<PagedResult<PromotionDto>> Handle(ListPromotionsQuery query, CancellationToken cancellationToken)
    {
        var decisions = db.YearPromotions.AsNoTracking();
        if (query.AcademicYear is { } year)
            decisions = decisions.Where(p => p.AcademicYear == year);
        if (query.DepartmentId is { } departmentId)
            decisions = decisions.Where(p => p.DepartmentId == departmentId);
        if (query.StudentId is { } studentId)
            decisions = decisions.Where(p => p.StudentId == studentId);
        if (query.Result is { } result)
            decisions = decisions.Where(p => p.Result == result);

        var total = await decisions.CountAsync(cancellationToken);
        var page = await decisions
            .OrderByDescending(p => p.DecidedAt).ThenBy(p => p.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PromotionDto>([.. page.Select(PromotionDto.From)], query.Paging.Page, query.Paging.PageSize, total);
    }
}
