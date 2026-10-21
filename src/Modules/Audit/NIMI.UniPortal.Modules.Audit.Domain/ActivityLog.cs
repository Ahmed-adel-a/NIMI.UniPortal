namespace NIMI.UniPortal.Modules.Audit.Domain;

/// <summary>
/// One sensitive action (P10.2): who did what to which record, when. Append-only; one row per producing event
/// (<see cref="SourceEventId"/> is unique, so a replayed event is stored once).
/// </summary>
public sealed class ActivityLog
{
    public const int MaxActionLength = 50;
    public const int MaxEntityTypeLength = 50;
    public const int MaxSummaryLength = 1000;

    private ActivityLog()
    {
    }

    public Guid Id { get; private set; }

    public Guid SourceEventId { get; private set; }

    /// <summary>Null when no signed-in user caused it (a background job, a payment provider callback).</summary>
    public Guid? ActorId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string Summary { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public static ActivityLog Record(
        Guid sourceEventId, Guid? actorId, string action, string entityType, Guid entityId, string summary, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(action) || action.Length > MaxActionLength)
            throw new ArgumentException($"An action of 1–{MaxActionLength} characters.", nameof(action));
        if (string.IsNullOrWhiteSpace(entityType) || entityType.Length > MaxEntityTypeLength)
            throw new ArgumentException($"An entity type of 1–{MaxEntityTypeLength} characters.", nameof(entityType));

        var text = (summary ?? string.Empty).Trim();
        return new ActivityLog
        {
            Id = Guid.CreateVersion7(),
            SourceEventId = sourceEventId,
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Summary = text.Length <= MaxSummaryLength ? text : text[..(MaxSummaryLength - 3)] + "...",
            OccurredAt = occurredAt,
        };
    }
}
