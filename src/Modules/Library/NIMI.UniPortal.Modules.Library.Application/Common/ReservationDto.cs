using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>A reservation; <see cref="Position"/> is the place in the queue while waiting (null otherwise).</summary>
public sealed record ReservationDto(
    Guid Id,
    Guid BookId,
    string Title,
    ReservationStatus Status,
    int? Position,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadyAt,
    DateTimeOffset? ExpiresAt);
