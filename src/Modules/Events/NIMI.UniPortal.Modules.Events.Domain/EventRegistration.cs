namespace NIMI.UniPortal.Modules.Events.Domain;

/// <summary>A member's registration at an event: one row per event and member, renewed after a cancellation.</summary>
public sealed class EventRegistration
{
    private EventRegistration()
    {
    }

    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    /// <summary>Users user id (cross-schema id, no FK).</summary>
    public Guid UserId { get; private set; }

    public EventRegistrationStatus Status { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public DateTimeOffset? AttendedAt { get; private set; }

    public static EventRegistration Register(Guid eventId, Guid userId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            UserId = userId,
            Status = EventRegistrationStatus.Registered,
            RegisteredAt = now,
        };

    public void Cancel(DateTimeOffset now)
    {
        if (Status != EventRegistrationStatus.Registered)
            throw new InvalidOperationException("Only an active registration can be cancelled.");

        Status = EventRegistrationStatus.Cancelled;
        CancelledAt = now;
    }

    public void Renew(DateTimeOffset now)
    {
        if (Status != EventRegistrationStatus.Cancelled)
            throw new InvalidOperationException("Only a cancelled registration can be renewed.");

        Status = EventRegistrationStatus.Registered;
        RegisteredAt = now;
        CancelledAt = null;
    }

    public void CheckIn(DateTimeOffset now)
    {
        if (Status != EventRegistrationStatus.Registered)
            throw new InvalidOperationException("Only a registered member can be checked in, once.");

        Status = EventRegistrationStatus.Attended;
        AttendedAt = now;
    }
}
