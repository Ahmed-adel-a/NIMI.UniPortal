using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.Events.Application.Common;

/// <summary>
/// An event as members and Student Affairs see it. <see cref="SeatsLeft"/> is null for unlimited events;
/// <see cref="MyStatus"/> is the caller's own registration status (null when they never registered).
/// </summary>
public sealed record EventDto(
    Guid Id,
    string Title,
    string Description,
    Guid? FacultyId,
    string Venue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? Capacity,
    DateTimeOffset RegistrationDeadline,
    EventStatus Status,
    int SeatsTaken,
    int? SeatsLeft,
    EventRegistrationStatus? MyStatus,
    string? CancellationReason,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt)
{
    public static EventDto From(CampusEvent e, int seatsTaken, EventRegistrationStatus? myStatus) =>
        new(e.Id, e.Title, e.Description, e.FacultyId, e.Venue, e.StartsAt, e.EndsAt, e.Capacity, e.RegistrationDeadline,
            e.Status, seatsTaken, e.Capacity is { } capacity ? Math.Max(0, capacity - seatsTaken) : null, myStatus,
            e.CancellationReason, e.PublishedAt, e.CreatedAt);
}
