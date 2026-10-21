namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

/// <summary>
/// A student's registration in one subject group of a term (P4.3, rule R4). The credit hours are a snapshot of the
/// subject at registration time. Dropping keeps the row (history) but frees the subject for a new registration.
/// </summary>
public sealed class CourseRegistration
{
    private CourseRegistration()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    /// <summary>Scheduling term id (cross-schema id, no FK).</summary>
    public Guid TermId { get; private set; }

    /// <summary>Scheduling subject id (cross-schema id, no FK).</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>Scheduling subject-group id (cross-schema id, no FK).</summary>
    public Guid SubjectGroupId { get; private set; }

    /// <summary>Credits at registration time — later catalog edits must not change history.</summary>
    public int CreditHours { get; private set; }

    public RegistrationStatus Status { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset? DroppedAt { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Decision D2 — self-registration is auto-approved, so a normal registration starts registered.</summary>
    public static CourseRegistration Register(
        Guid studentId,
        Guid termId,
        Guid subjectId,
        Guid subjectGroupId,
        int creditHours,
        DateTimeOffset now) =>
        Create(studentId, termId, subjectId, subjectGroupId, creditHours, RegistrationStatus.Registered, now);

    /// <summary>The military-education flow (P5.5) reserves a place until its fee is paid.</summary>
    public static CourseRegistration CreatePendingPayment(
        Guid studentId,
        Guid termId,
        Guid subjectId,
        Guid subjectGroupId,
        int creditHours,
        DateTimeOffset now) =>
        Create(studentId, termId, subjectId, subjectGroupId, creditHours, RegistrationStatus.PendingPayment, now);

    /// <summary>Rule R10: only a registered row can be dropped, and only once.</summary>
    public void Drop(DateTimeOffset now)
    {
        if (Status != RegistrationStatus.Registered)
            throw new InvalidOperationException("Only a registered course can be dropped.");

        Status = RegistrationStatus.Dropped;
        DroppedAt = now;
        UpdatedAt = now;
    }

    /// <summary>P5.5: a pending military-education registration becomes registered when its fee is paid.</summary>
    public void ConfirmPayment(DateTimeOffset now)
    {
        if (Status != RegistrationStatus.PendingPayment)
            throw new InvalidOperationException("Only a pending payment registration can be confirmed.");

        Status = RegistrationStatus.Registered;
        ConfirmedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// P9.3b — a published grade completes or fails a confirmed registration; a corrected grade may flip it back.
    /// Pending and dropped rows never receive results.
    /// </summary>
    public void RecordResult(bool passed, DateTimeOffset now)
    {
        if (Status is not (RegistrationStatus.Registered or RegistrationStatus.Completed or RegistrationStatus.Failed))
            throw new InvalidOperationException("Only a confirmed registration receives a result.");

        Status = passed ? RegistrationStatus.Completed : RegistrationStatus.Failed;
        UpdatedAt = now;
    }

    private static CourseRegistration Create(
        Guid studentId,
        Guid termId,
        Guid subjectId,
        Guid subjectGroupId,
        int creditHours,
        RegistrationStatus status,
        DateTimeOffset now)
    {
        if (creditHours < 0)
            throw new ArgumentOutOfRangeException(nameof(creditHours), creditHours, "Must be zero or more.");

        return new CourseRegistration
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            TermId = termId,
            SubjectId = subjectId,
            SubjectGroupId = subjectGroupId,
            CreditHours = creditHours,
            Status = status,
            RegisteredAt = now,
            CreatedAt = now,
        };
    }
}
