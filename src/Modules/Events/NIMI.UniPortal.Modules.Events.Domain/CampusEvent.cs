using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.Events.Domain;

/// <summary>
/// A university or faculty event run by Student Affairs (P11, decision EVENTSDESIGN). Members register while it is
/// published and before its deadline; check-in runs from an hour before the start until the end. Cancelling is final.
/// </summary>
public sealed class CampusEvent
{
    public static readonly TimeSpan CheckInOpensBefore = TimeSpan.FromHours(1);

    private CampusEvent()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    /// <summary>Users faculty id (cross-schema id, no FK); null = the whole university.</summary>
    public Guid? FacultyId { get; private set; }

    public string Venue { get; private set; } = string.Empty;

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Null = unlimited.</summary>
    public int? Capacity { get; private set; }

    public DateTimeOffset RegistrationDeadline { get; private set; }

    public EventStatus Status { get; private set; }

    public string? CancellationReason { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static CampusEvent Create(
        string title,
        string description,
        Guid? facultyId,
        string venue,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int? capacity,
        DateTimeOffset registrationDeadline,
        Guid createdBy,
        DateTimeOffset now)
    {
        var campusEvent = new CampusEvent
        {
            Id = Guid.CreateVersion7(),
            Status = EventStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = now,
        };
        campusEvent.Apply(title, description, facultyId, venue, startsAt, endsAt, capacity, registrationDeadline);
        return campusEvent;
    }

    /// <summary>
    /// Replaces the details (not allowed once cancelled). Returns true when registrants must be told: the event is
    /// published and its time or venue changed (rule R4).
    /// </summary>
    public bool Update(
        string title,
        string description,
        Guid? facultyId,
        string venue,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int? capacity,
        DateTimeOffset registrationDeadline,
        DateTimeOffset now)
    {
        if (Status == EventStatus.Cancelled)
            throw new InvalidOperationException("A cancelled event can't be changed.");

        var (oldVenue, oldStart, oldEnd) = (Venue, StartsAt, EndsAt);
        Apply(title, description, facultyId, venue, startsAt, endsAt, capacity, registrationDeadline);
        UpdatedAt = now;
        return Status == EventStatus.Published && (Venue != oldVenue || StartsAt != oldStart || EndsAt != oldEnd);
    }

    public void Publish(DateTimeOffset now)
    {
        if (Status != EventStatus.Draft)
            throw new InvalidOperationException("Only a draft event can be published.");

        Status = EventStatus.Published;
        PublishedAt = now;
        UpdatedAt = now;
    }

    public void Cancel(string reason, DateTimeOffset now)
    {
        if (Status == EventStatus.Cancelled)
            throw new InvalidOperationException("This event is already cancelled.");

        CancellationReason = EventText.Required(reason, EventText.MaxReasonLength, nameof(reason));
        Status = EventStatus.Cancelled;
        CancelledAt = now;
        UpdatedAt = now;
    }

    public bool IsOpenForRegistration(DateTimeOffset now) =>
        Status == EventStatus.Published && now <= RegistrationDeadline && now < StartsAt;

    public bool IsCheckInOpen(DateTimeOffset now) => now >= StartsAt - CheckInOpensBefore && now <= EndsAt;

    private void Apply(
        string title,
        string description,
        Guid? facultyId,
        string venue,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int? capacity,
        DateTimeOffset registrationDeadline)
    {
        Title = EventText.Required(title, EventText.MaxTitleLength, nameof(title));
        Description = EventText.Required(description, EventText.MaxDescriptionLength, nameof(description));
        Venue = EventText.Required(venue, EventText.MaxVenueLength, nameof(venue));
        CheckSchedule(startsAt, endsAt, capacity, registrationDeadline);
        FacultyId = facultyId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Capacity = capacity;
        RegistrationDeadline = registrationDeadline;
    }

    // Never inlined: the host recognises a domain rule (400) by the throwing method's assembly.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckSchedule(DateTimeOffset startsAt, DateTimeOffset endsAt, int? capacity, DateTimeOffset registrationDeadline)
    {
        if (endsAt <= startsAt)
            throw new ArgumentException("The event must end after it starts.", nameof(endsAt));
        if (registrationDeadline > startsAt)
            throw new ArgumentException("Registration must close no later than the start.", nameof(registrationDeadline));
        if (capacity is <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "At least one seat, or leave it empty for unlimited.");
    }
}
