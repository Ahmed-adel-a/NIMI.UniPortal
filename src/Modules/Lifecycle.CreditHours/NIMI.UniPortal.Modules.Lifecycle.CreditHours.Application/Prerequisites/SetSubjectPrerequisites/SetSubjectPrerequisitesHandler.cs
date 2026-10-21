using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites.SetSubjectPrerequisites;

internal sealed class SetSubjectPrerequisitesHandler(ICreditHoursDbContext db, ISchedulingApi scheduling)
    : IRequestHandler<SetSubjectPrerequisitesCommand, SubjectPrerequisitesDto>
{
    /// <summary>The JSON field every prerequisite problem is reported on (phase §6b).</summary>
    private const string Field = "prerequisiteSubjectIds";

    public async Task<SubjectPrerequisitesDto> Handle(
        SetSubjectPrerequisitesCommand command, CancellationToken cancellationToken)
    {
        var subject = await scheduling.GetSubjectAsync(command.SubjectId, cancellationToken)
            ?? throw new NotFoundException($"No subject with id '{command.SubjectId}'.");

        var ids = command.PrerequisiteSubjectIds.Distinct().ToList();

        if (ids.Contains(subject.Id))
            throw ValidationErrors.For(Field, "A subject cannot require itself.");

        var references = new Dictionary<Guid, SubjectRef>(ids.Count);
        foreach (var id in ids)
        {
            var prerequisite = await scheduling.GetSubjectAsync(id, cancellationToken)
                ?? throw ValidationErrors.For(Field, $"Unknown subject '{id}'.");
            references[id] = prerequisite;
        }

        // Only two-node cycles are rejected in this phase (phase §6b): B requires A, so A cannot require B.
        if (ids.Count > 0 && await db.SubjectPrerequisites.AsNoTracking()
                .AnyAsync(
                    p => ids.Contains(p.SubjectId) && p.PrerequisiteSubjectId == subject.Id,
                    cancellationToken))
            throw ValidationErrors.For(Field, "A subject cannot require one that requires it.");

        var existing = await db.SubjectPrerequisites
            .Where(p => p.SubjectId == subject.Id)
            .ToListAsync(cancellationToken);
        var wanted = ids.ToHashSet();
        var existingIds = existing.Select(p => p.PrerequisiteSubjectId).ToHashSet();

        db.SubjectPrerequisites.RemoveRange(existing.Where(p => !wanted.Contains(p.PrerequisiteSubjectId)));
        foreach (var id in ids.Where(id => !existingIds.Contains(id)))
            db.SubjectPrerequisites.Add(SubjectPrerequisite.Create(subject.Id, id));

        var prerequisites = references.Values
            .OrderBy(s => s.Code, StringComparer.Ordinal)
            .Select(s => new SubjectPrerequisiteDto(s.Id, s.Code, s.Name, s.NameAr))
            .ToList();

        return new SubjectPrerequisitesDto(subject.Id, prerequisites);
    }
}
