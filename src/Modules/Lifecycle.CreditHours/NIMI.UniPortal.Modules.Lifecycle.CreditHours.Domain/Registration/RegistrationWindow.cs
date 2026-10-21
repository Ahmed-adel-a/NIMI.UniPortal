namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

/// <summary>
/// The registration window of one term (P4.2, decision PREREQ). A term has at most one window (unique index); the
/// window carries that term's credit limits. Registration and dropping are allowed only while
/// <see cref="OpensAt"/> ≤ now &lt; <see cref="ClosesAt"/>.
/// </summary>
public sealed class RegistrationWindow
{
    private RegistrationWindow()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Scheduling term id — a cross-schema id, indexed and unique, never a foreign key.</summary>
    public Guid TermId { get; private set; }

    public DateTimeOffset OpensAt { get; private set; }

    public DateTimeOffset ClosesAt { get; private set; }

    public int MinCredits { get; private set; }

    public int MaxCredits { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static RegistrationWindow Create(
        Guid termId,
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        int minCredits,
        int maxCredits,
        DateTimeOffset now)
    {
        Validate(opensAt, closesAt, minCredits, maxCredits);

        return new RegistrationWindow
        {
            Id = Guid.CreateVersion7(),
            TermId = termId,
            OpensAt = opensAt,
            ClosesAt = closesAt,
            MinCredits = minCredits,
            MaxCredits = maxCredits,
            CreatedAt = now,
        };
    }

    /// <summary>Moves the window; the term never changes. The same date and credit rules apply.</summary>
    public void Update(
        DateTimeOffset opensAt,
        DateTimeOffset closesAt,
        int minCredits,
        int maxCredits,
        DateTimeOffset now)
    {
        Validate(opensAt, closesAt, minCredits, maxCredits);

        OpensAt = opensAt;
        ClosesAt = closesAt;
        MinCredits = minCredits;
        MaxCredits = maxCredits;
        UpdatedAt = now;
    }

    /// <summary>Rules R1/R10: open from <see cref="OpensAt"/> (inclusive) until <see cref="ClosesAt"/> (exclusive).</summary>
    public bool IsOpen(DateTimeOffset now) => OpensAt <= now && now < ClosesAt;

    private static void Validate(
        DateTimeOffset opensAt, DateTimeOffset closesAt, int minCredits, int maxCredits)
    {
        if (closesAt <= opensAt)
            throw new ArgumentException("Must be after 'opensAt'.", nameof(closesAt));

        if (minCredits < 0)
            throw new ArgumentOutOfRangeException(nameof(minCredits), minCredits, "Must be zero or more.");

        if (minCredits > maxCredits || maxCredits > 40)
            throw new ArgumentOutOfRangeException(
                nameof(maxCredits), maxCredits, "Must be between 'minCredits' and 40.");
    }
}
