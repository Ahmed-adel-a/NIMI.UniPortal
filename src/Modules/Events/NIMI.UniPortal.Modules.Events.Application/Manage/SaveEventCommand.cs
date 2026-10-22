using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

/// <summary>Student Affairs creates a draft (<see cref="Id"/> null) or replaces an event's details.</summary>
public sealed record SaveEventCommand(
    Guid? Id,
    string? Title,
    string? Description,
    Guid? FacultyId,
    string? Venue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? Capacity,
    DateTimeOffset RegistrationDeadline,
    Guid AdminId) : ICommand<EventDto>;
