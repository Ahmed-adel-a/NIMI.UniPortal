using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Results;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

internal sealed class GetMyStandingHandler(ISemesterDbContext db, ISchedulingApi scheduling)
    : IRequestHandler<GetMyStandingQuery, MyStandingDto>
{
    public async Task<MyStandingDto> Handle(GetMyStandingQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsStudent)
            throw new ForbiddenException("Only students have a standing.");

        var standing = await db.Standings.AsNoTracking()
            .SingleOrDefaultAsync(s => s.StudentId == query.StudentId, cancellationToken)
            ?? throw new NotFoundException("You have not been placed in a year level yet.");
        var finalLevel = await db.StudyPlans.AsNoTracking()
            .Where(p => p.DepartmentId == standing.DepartmentId)
            .Select(p => (int?)p.Levels)
            .SingleOrDefaultAsync(cancellationToken) ?? standing.Level;

        var carried = new List<CarriedSubjectDto>();
        foreach (var owed in await StudentRecord.OutstandingAsync(db, query.StudentId, cancellationToken))
        {
            var subject = await scheduling.GetSubjectAsync(owed.SubjectId, cancellationToken);
            carried.Add(new CarriedSubjectDto(
                owed.SubjectId, subject?.Code ?? string.Empty, subject?.Name ?? string.Empty, subject?.NameAr ?? string.Empty, owed.Level));
        }

        var last = await db.YearPromotions.AsNoTracking()
            .Where(p => p.StudentId == query.StudentId)
            .OrderByDescending(p => p.DecidedAt)
            .Select(p => new LastDecisionDto(p.AcademicYear, p.Result, p.FromLevel, p.ToLevel, p.Reason, p.DecidedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return new MyStandingDto(
            standing.StudentId, standing.DepartmentId, standing.Level, finalLevel, standing.YearsInLevel, standing.Status,
            [.. carried.OrderBy(c => c.Level).ThenBy(c => c.Code, StringComparer.Ordinal)], last);
    }
}
