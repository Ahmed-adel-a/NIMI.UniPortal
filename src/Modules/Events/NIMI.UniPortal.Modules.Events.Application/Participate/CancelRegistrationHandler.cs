using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

internal sealed class CancelRegistrationHandler(IEventsDbContext db, EventAudience audience, IEventSeats seats, TimeProvider clock)
    : IRequestHandler<CancelRegistrationCommand, Unit>
{
    public async Task<Unit> Handle(CancelRegistrationCommand command, CancellationToken cancellationToken)
    {
        var campusEvent = await audience.VisibleAsync(command.EventId, command.Caller, cancellationToken);
        var registration = await db.Registrations
            .SingleOrDefaultAsync(r => r.EventId == campusEvent.Id && r.UserId == command.Caller.UserId, cancellationToken);
        if (registration is null || registration.Status != EventRegistrationStatus.Registered)
            throw new NotFoundException("You have no active registration for this event.");

        var now = clock.GetUtcNow();
        if (now >= campusEvent.StartsAt)
            throw new ConflictException("The event has started; the registration can no longer be cancelled.");

        if (campusEvent.Capacity is not null)
        {
            await seats.BeginAsync(cancellationToken);
            await seats.ReleaseAsync(campusEvent.Id, cancellationToken);
        }

        registration.Cancel(now);
        return Unit.Value;
    }
}
