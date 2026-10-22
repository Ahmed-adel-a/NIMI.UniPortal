namespace NIMI.UniPortal.Modules.Events.Domain;

/// <summary>An event is drafted, then published to its audience, and may be cancelled (final).</summary>
public enum EventStatus
{
    Draft,
    Published,
    Cancelled,
}
