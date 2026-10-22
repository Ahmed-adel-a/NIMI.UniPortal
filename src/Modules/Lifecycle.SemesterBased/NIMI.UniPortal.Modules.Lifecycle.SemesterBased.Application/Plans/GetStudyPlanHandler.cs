using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

internal sealed class GetStudyPlanHandler(ISemesterDbContext db, ISchedulingApi scheduling)
    : IRequestHandler<GetStudyPlanQuery, StudyPlanDto>
{
    public async Task<StudyPlanDto> Handle(GetStudyPlanQuery query, CancellationToken cancellationToken)
    {
        var plan = await db.StudyPlans.AsNoTracking()
            .Include(p => p.Subjects)
            .SingleOrDefaultAsync(p => p.DepartmentId == query.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("This department has no study plan yet.");

        return await StudyPlanReader.ToDtoAsync(plan, scheduling, cancellationToken);
    }
}
