using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

/// <summary>
/// A cancelled event can't change (409); the capacity never drops below the seats already taken (400). A new time or
/// venue of a published event notifies its registrants (R4).
/// </summary>
internal sealed class SaveEventHandler(
    IEventsDbContext db, IUsersApi users, EventPresenter presenter, RegistrantNotices notices, TimeProvider clock)
    : IRequestHandler<SaveEventCommand, EventDto>
{
    public async Task<EventDto> Handle(SaveEventCommand command, CancellationToken cancellationToken)
    {
        if (command.FacultyId is { } facultyId && await users.GetFacultyAsync(facultyId, cancellationToken) is null)
            throw ValidationErrors.For("facultyId", "Unknown faculty.");

        var now = clock.GetUtcNow();
        if (command.Id is not { } id)
        {
            var created = CampusEvent.Create(
                command.Title ?? string.Empty, command.Description ?? string.Empty, command.FacultyId, command.Venue ?? string.Empty,
                command.StartsAt, command.EndsAt, command.Capacity, command.RegistrationDeadline, command.AdminId, now);
            db.Events.Add(created);
            return EventDto.From(created, 0, null);
        }

        var campusEvent = await db.Events.SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException("Event not found.");
        if (campusEvent.Status == EventStatus.Cancelled)
            throw new ConflictException("A cancelled event can't be changed.");

        var taken = await db.Registrations.CountAsync(
            r => r.EventId == id && (r.Status == EventRegistrationStatus.Registered || r.Status == EventRegistrationStatus.Attended),
            cancellationToken);
        if (command.Capacity is { } capacity && capacity < taken)
            throw ValidationErrors.For("capacity", $"{taken} seats are already taken.");

        var notify = campusEvent.Update(
            command.Title ?? string.Empty, command.Description ?? string.Empty, command.FacultyId, command.Venue ?? string.Empty,
            command.StartsAt, command.EndsAt, command.Capacity, command.RegistrationDeadline, now);
        if (notify)
            await notices.ChangedAsync(campusEvent, now, cancellationToken);

        return await presenter.PresentAsync(campusEvent, command.AdminId, cancellationToken);
    }
}
