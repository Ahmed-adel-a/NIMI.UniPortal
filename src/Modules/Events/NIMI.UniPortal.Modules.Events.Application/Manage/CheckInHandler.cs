using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

internal sealed class CheckInHandler(IEventsDbContext db, TimeProvider clock) : IRequestHandler<CheckInCommand, RegistrationDto>
{
    public async Task<RegistrationDto> Handle(CheckInCommand command, CancellationToken cancellationToken)
    {
        var campusEvent = await db.Events.AsNoTracking().SingleOrDefaultAsync(e => e.Id == command.EventId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");
        var registration = await db.Registrations
            .SingleOrDefaultAsync(r => r.EventId == command.EventId && r.UserId == command.UserId, cancellationToken);
        if (registration is null || registration.Status == EventRegistrationStatus.Cancelled)
            throw new NotFoundException("This member is not registered for the event.");

        var now = clock.GetUtcNow();
        if (campusEvent.Status != EventStatus.Published)
            throw new ConflictException("Only a published event takes check-ins.");
        if (!campusEvent.IsCheckInOpen(now))
            throw new ConflictException("Check-in opens an hour before the start and closes at the end.");
        if (registration.Status == EventRegistrationStatus.Attended)
            throw new ConflictException("This member is already checked in.");

        registration.CheckIn(now);
        return RegistrationDto.From(registration);
    }
}
