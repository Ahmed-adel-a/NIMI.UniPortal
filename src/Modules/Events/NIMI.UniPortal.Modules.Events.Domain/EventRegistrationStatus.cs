namespace NIMI.UniPortal.Modules.Events.Domain;

/// <summary>A member's place at an event: registered, given back (cancelled) or checked in (attended).</summary>
public enum EventRegistrationStatus
{
    Registered,
    Cancelled,
    Attended,
}
