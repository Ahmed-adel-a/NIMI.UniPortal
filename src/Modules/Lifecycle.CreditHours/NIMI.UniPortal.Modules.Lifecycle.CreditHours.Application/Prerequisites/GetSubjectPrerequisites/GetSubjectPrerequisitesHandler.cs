using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites.GetSubjectPrerequisites;

internal sealed class GetSubjectPrerequisitesHandler(ICreditHoursDbContext db, ISchedulingApi scheduling)
    : IRequestHandler<GetSubjectPrerequisitesQuery, SubjectPrerequisitesDto>
{
    public async Task<SubjectPrerequisitesDto> Handle(
        GetSubjectPrerequisitesQuery query, CancellationToken cancellationToken)
    {
        var subject = await scheduling.GetSubjectAsync(query.SubjectId, cancellationToken)
            ?? throw new NotFoundException($"No subject with id '{query.SubjectId}'.");

        var prerequisiteIds = await db.SubjectPrerequisites.AsNoTracking()
            .Where(p => p.SubjectId == query.SubjectId)
            .Select(p => p.PrerequisiteSubjectId)
            .ToListAsync(cancellationToken);

        var prerequisites = new List<SubjectPrerequisiteDto>(prerequisiteIds.Count);
        foreach (var prerequisiteId in prerequisiteIds)
        {
            var prerequisite = await scheduling.GetSubjectAsync(prerequisiteId, cancellationToken);
            if (prerequisite is not null)
                prerequisites.Add(ToDto(prerequisite));
        }

        return new SubjectPrerequisitesDto(
            subject.Id, prerequisites.OrderBy(p => p.Code, StringComparer.Ordinal).ToList());
    }

    private static SubjectPrerequisiteDto ToDto(SubjectRef subject) =>
        new(subject.Id, subject.Code, subject.Name, subject.NameAr);
}
