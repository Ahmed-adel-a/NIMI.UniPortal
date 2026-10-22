using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

internal sealed class GetCohortsHandler(ISemesterDbContext db) : IRequestHandler<GetCohortsQuery, IReadOnlyList<CohortLevelDto>>
{
    public async Task<IReadOnlyList<CohortLevelDto>> Handle(GetCohortsQuery query, CancellationToken cancellationToken)
    {
        var plan = await db.StudyPlans.AsNoTracking()
            .SingleOrDefaultAsync(p => p.DepartmentId == query.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("This department has no study plan yet.");

        var counts = await db.Standings.AsNoTracking()
            .Where(s => s.DepartmentId == query.DepartmentId)
            .GroupBy(s => new { s.Level, s.Status })
            .Select(g => new { g.Key.Level, g.Key.Status, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int Count(int level, StandingStatus status) =>
            counts.Where(c => c.Level == level && c.Status == status).Sum(c => c.Count);

        return [.. Enumerable.Range(1, plan.Levels).Select(level =>
        {
            var active = Count(level, StandingStatus.Active);
            var repeating = Count(level, StandingStatus.Repeating);
            var external = Count(level, StandingStatus.External);
            var onHold = Count(level, StandingStatus.OnHold);
            return new CohortLevelDto(level, active, repeating, external, onHold, active + repeating + external + onHold);
        })];
    }
}
