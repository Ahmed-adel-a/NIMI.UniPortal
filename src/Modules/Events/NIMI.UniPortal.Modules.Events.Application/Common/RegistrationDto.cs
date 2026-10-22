using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.Events.Application.Common;

/// <summary>A member's registration at an event.</summary>
public sealed record RegistrationDto(
    Guid Id,
    Guid EventId,
    Guid UserId,
    EventRegistrationStatus Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? AttendedAt)
{
    public static RegistrationDto From(EventRegistration r) =>
        new(r.Id, r.EventId, r.UserId, r.Status, r.RegisteredAt, r.CancelledAt, r.AttendedAt);
}
