using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.ResyncMilitaryEducation;

/// <summary>
/// P5.5 — the recovery path after Student Affairs defines a military-education fee item. It republishes the request
/// for every still-pending registration of the term; the Payments consumer creates only missing fees.
/// </summary>
internal sealed class ResyncMilitaryEducationHandler(
    ICreditHoursDbContext db,
    ISchedulingApi scheduling,
    IEventCollector events,
    TimeProvider clock)
    : IRequestHandler<ResyncMilitaryEducationCommand, MilitaryEducationResyncDto>
{
    public async Task<MilitaryEducationResyncDto> Handle(
        ResyncMilitaryEducationCommand command, CancellationToken cancellationToken)
    {
        var term = await scheduling.GetTermAsync(command.TermId, cancellationToken)
            ?? throw new NotFoundException($"No term with id '{command.TermId}'.");

        var pending = await db.CourseRegistrations.AsNoTracking()
            .Where(r => r.TermId == term.Id && r.Status == RegistrationStatus.PendingPayment)
            .OrderBy(r => r.RegisteredAt)
            .ThenBy(r => r.Id)
            .Select(r => new { r.StudentId, r.Id, r.SubjectId })
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        foreach (var registration in pending)
        {
            events.Add(new MilitaryEducationRequested(
                Guid.CreateVersion7(),
                now,
                registration.StudentId,
                registration.Id,
                registration.SubjectId,
                term.Id));
        }

        return new MilitaryEducationResyncDto(pending.Count);
    }
}
