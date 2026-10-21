namespace NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

/// <summary>
/// An academic term (P3.1). Its dates and week count are editable only while planned (rule R2); activating it
/// closes the previously active term (rule R1, also enforced by a partial unique index).
/// </summary>
public sealed class Term
{
    private Term()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Start year of the academic year: 2026 = 2026/2027.</summary>
    public int AcademicYear { get; private set; }

    public TermNumber Number { get; private set; }

    public DateOnly StartsOn { get; private set; }

    public DateOnly EndsOn { get; private set; }

    /// <summary>Teaching weeks in the term (1–30); the university's own calendar, not a computed value.</summary>
    public int WeekCount { get; private set; }

    public TermStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Term Create(
        int academicYear, TermNumber number, DateOnly startsOn, DateOnly endsOn, int weekCount, DateTimeOffset now)
    {
        var term = new Term
        {
            Id = Guid.CreateVersion7(),
            Status = TermStatus.Planned,
            CreatedAt = now,
        };
        term.Apply(academicYear, number, startsOn, endsOn, weekCount);
        return term;
    }

    /// <summary>Edits the calendar. Only a planned term can change (rule R2).</summary>
    public void Update(
        int academicYear, TermNumber number, DateOnly startsOn, DateOnly endsOn, int weekCount, DateTimeOffset now)
    {
        if (Status != TermStatus.Planned)
            throw new InvalidOperationException("Only a planned term can be edited.");

        Apply(academicYear, number, startsOn, endsOn, weekCount);
        UpdatedAt = now;
    }

    /// <summary>Planned → active. The handler first closes the previously active term (rule R1).</summary>
    public void Activate(DateTimeOffset now)
    {
        if (Status != TermStatus.Planned)
            throw new InvalidOperationException("Only a planned term can be activated.");

        Status = TermStatus.Active;
        UpdatedAt = now;
    }

    /// <summary>Active → closed. Closing a planned or closed term is a state error.</summary>
    public void Close(DateTimeOffset now)
    {
        if (Status != TermStatus.Active)
            throw new InvalidOperationException("Only an active term can be closed.");

        Status = TermStatus.Closed;
        UpdatedAt = now;
    }

    /// <summary>Both ends are inside the term (inclusive) — used by timetable date rules.</summary>
    public bool Contains(DateOnly date) => date >= StartsOn && date <= EndsOn;

    private void Apply(int academicYear, TermNumber number, DateOnly startsOn, DateOnly endsOn, int weekCount)
    {
        if (academicYear is < 2000 or > 2100)
            throw new ArgumentException("Between 2000 and 2100.", nameof(academicYear));

        if (!Enum.IsDefined(number))
            throw new ArgumentException("Unknown term number.", nameof(number));

        if (endsOn <= startsOn)
            throw new ArgumentException("Must be after the start date.", nameof(endsOn));

        if (weekCount is < 1 or > 30)
            throw new ArgumentException("Between 1 and 30.", nameof(weekCount));

        AcademicYear = academicYear;
        Number = number;
        StartsOn = startsOn;
        EndsOn = endsOn;
        WeekCount = weekCount;
    }
}
