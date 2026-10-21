namespace NIMI.UniPortal.Modules.Audit.Application.Activity;

/// <summary>One activity row; <see cref="ActorName"/> comes from Users (null for unknown or system actors).</summary>
public sealed record ActivityItemDto(
    Guid Id,
    Guid? ActorId,
    string? ActorName,
    string Action,
    string EntityType,
    Guid EntityId,
    string Summary,
    DateTimeOffset OccurredAt);
