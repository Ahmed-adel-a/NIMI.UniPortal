using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

/// <summary>
/// R1 open (published, before the deadline and the start) · R2 the seat is taken atomically — a lost race is 409 "The
/// event is full." and nothing is saved · R3 one registration per member (a cancelled one is renewed).
/// </summary>
internal sealed class RegisterHandler(IEventsDbContext db, EventAudience audience, IEventSeats seats, TimeProvider clock)
    : IRequestHandler<RegisterCommand, RegistrationDto>
{
    public async Task<RegistrationDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (command.Caller.IsAdmin)
            throw new ForbiddenException("Student Affairs runs events; members register.");

        var campusEvent = await audience.VisibleAsync(command.EventId, command.Caller, cancellationToken);
        var now = clock.GetUtcNow();
        if (!campusEvent.IsOpenForRegistration(now))
            throw new ConflictException(campusEvent.Status == EventStatus.Cancelled
                ? "This event was cancelled."
                : "Registration for this event is closed.");

        var registration = await db.Registrations
            .SingleOrDefaultAsync(r => r.EventId == campusEvent.Id && r.UserId == command.Caller.UserId, cancellationToken);
        if (registration is not null && registration.Status != EventRegistrationStatus.Cancelled)
            throw new ConflictException("You are already registered for this event.");

        if (campusEvent.Capacity is { } capacity)
        {
            await seats.BeginAsync(cancellationToken);
            if (!await seats.TryTakeAsync(campusEvent.Id, capacity, cancellationToken))
            {
                await seats.RollbackAsync(cancellationToken);
                throw new ConflictException("The event is full.");
            }
        }

        if (registration is null)
        {
            registration = EventRegistration.Register(campusEvent.Id, command.Caller.UserId, now);
            db.Registrations.Add(registration);
        }
        else
        {
            registration.Renew(now);
        }

        return RegistrationDto.From(registration);
    }
}
