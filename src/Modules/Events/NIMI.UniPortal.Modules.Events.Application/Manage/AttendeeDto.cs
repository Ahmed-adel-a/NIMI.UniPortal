using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

/// <summary>One registrant on Student Affairs' attendee list (name and code come from Users).</summary>
public sealed record AttendeeDto(
    Guid UserId,
    string FullName,
    string? StudentCode,
    EventRegistrationStatus Status,
    DateTimeOffset RegisteredAt,
    DateTimeOffset? AttendedAt);
