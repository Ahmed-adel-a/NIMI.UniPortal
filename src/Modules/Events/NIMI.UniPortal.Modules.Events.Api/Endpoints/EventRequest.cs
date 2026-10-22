namespace NIMI.UniPortal.Modules.Events.Api.Endpoints;

/// <summary>POST /api/admin/events and PUT /api/admin/events/{id}.</summary>
public sealed record EventRequest(
    string? Title,
    string? Description,
    Guid? FacultyId,
    string? Venue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? Capacity,
    DateTimeOffset RegistrationDeadline);
