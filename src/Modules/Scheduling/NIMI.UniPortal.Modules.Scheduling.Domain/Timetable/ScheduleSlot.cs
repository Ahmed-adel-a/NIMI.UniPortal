using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;

namespace NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

/// <summary>
/// A weekly/biweekly/one-off timetable slot (P3.5, rules R7–R9). The calendar rules live here:
/// <see cref="OccurrencesBetween"/> says which real dates the slot takes place on and <see cref="OverlapsInTime"/>/
/// <see cref="ConflictsWith"/> decide clashes without asking the database.
/// </summary>
public sealed class ScheduleSlot
{
    private ScheduleSlot()
    {
    }

    public Guid Id { get; private set; }

    public Guid SubjectGroupId { get; private set; }

    public SubjectGroup SubjectGroup { get; private set; } = null!;

    public Guid FacilityId { get; private set; }

    public Facility Facility { get; private set; } = null!;

    /// <summary>Users account id of the professor named on the slot; null = only the teaching assignment says who.</summary>
    public Guid? StaffId { get; private set; }

    /// <summary>Product day: 0 = Saturday … 6 = Friday.</summary>
    public int DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public Recurrence Recurrence { get; private set; }

    public DateOnly ValidFrom { get; private set; }

    /// <summary>Never null: the term end for recurring slots, <see cref="ValidFrom"/> for one-off slots.</summary>
    public DateOnly ValidUntil { get; private set; }

    public bool IsCancelled { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a slot. Checks in this order (first failure wins, rule R7): <c>dayOfWeek</c> → <c>endTime</c> →
    /// <c>validFrom</c> (inside the term, and on the day for <c>once</c>) → <c>validUntil</c> (defaults to the term
    /// end / the start date, must lie inside the term and not precede <c>validFrom</c>; <c>once</c> lasts one day).
    /// </summary>
    public static ScheduleSlot Create(
        SubjectGroup group,
        Facility facility,
        Guid? staffId,
        int dayOfWeek,
        TimeOnly start,
        TimeOnly end,
        Recurrence recurrence,
        DateOnly validFrom,
        DateOnly? validUntil,
        Term term,
        Guid createdBy,
        DateTimeOffset now)
    {
        var slot = new ScheduleSlot
        {
            Id = Guid.CreateVersion7(),
            CreatedBy = createdBy,
            CreatedAt = now,
        };
        slot.Set(group, facility, staffId, dayOfWeek, start, end, recurrence, validFrom, validUntil, term);
        return slot;
    }

    /// <summary>Re-applies the same rules; the handler excludes the slot itself from its conflict query.</summary>
    public void Update(
        SubjectGroup group,
        Facility facility,
        Guid? staffId,
        int dayOfWeek,
        TimeOnly start,
        TimeOnly end,
        Recurrence recurrence,
        DateOnly validFrom,
        DateOnly? validUntil,
        Term term,
        DateTimeOffset now)
    {
        Set(group, facility, staffId, dayOfWeek, start, end, recurrence, validFrom, validUntil, term);
        UpdatedAt = now;
    }

    /// <summary>Rule R9: a cancelled slot never conflicts and has no occurrences.</summary>
    public void Cancel(DateTimeOffset now)
    {
        IsCancelled = true;
        UpdatedAt = now;
    }

    /// <summary>
    /// The real dates this slot takes place on inside <paramref name="from"/>..<paramref name="to"/> (inclusive):
    /// the first matching day on/after <c>validFrom</c>, then every 7 or 14 days until <c>validUntil</c>; a
    /// <c>once</c> slot only on <c>validFrom</c>. Empty when cancelled.
    /// </summary>
    public IReadOnlyList<DateOnly> OccurrencesBetween(DateOnly from, DateOnly to)
    {
        if (IsCancelled || to < ValidFrom || from > ValidUntil)
            return [];

        if (Recurrence == Recurrence.Once)
            return ValidFrom >= from && ValidFrom <= to ? [ValidFrom] : [];

        var step = Recurrence == Recurrence.Biweekly ? 14 : 7;
        var dates = new List<DateOnly>();
        for (var date = FirstOccurrenceOnOrAfter(ValidFrom); date <= ValidUntil; date = date.AddDays(step))
        {
            if (date >= from && date <= to)
                dates.Add(date);
        }

        return dates;
    }

    /// <summary>
    /// Neither slot cancelled, same product day, overlapping wall-clock times (touching ends are not an overlap)
    /// and at least one shared occurrence date — this covers date ranges, <c>once</c> and biweekly parity.
    /// </summary>
    public bool OverlapsInTime(ScheduleSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (IsCancelled || other.IsCancelled)
            return false;

        if (DayOfWeek != other.DayOfWeek)
            return false;

        if (!(StartTime < other.EndTime && other.StartTime < EndTime))
            return false;

        var start = ValidFrom > other.ValidFrom ? ValidFrom : other.ValidFrom;
        var end = ValidUntil < other.ValidUntil ? ValidUntil : other.ValidUntil;
        if (start > end)
            return false;

        var mine = OccurrencesBetween(start, end);
        if (mine.Count == 0)
            return false;

        var dates = mine.ToHashSet();
        return other.OccurrencesBetween(start, end).Any(dates.Contains);
    }

    /// <summary>Rule R8: overlap plus a shared facility, group, or (both non-null) staff member.</summary>
    public bool ConflictsWith(ScheduleSlot other) =>
        OverlapsInTime(other) &&
        (FacilityId == other.FacilityId ||
         SubjectGroupId == other.SubjectGroupId ||
         (StaffId is not null && StaffId == other.StaffId));

    /// <summary>Saturday = 0, Sunday = 1 … Friday = 6.</summary>
    public static int ProductDay(DateOnly date) => ((int)date.DayOfWeek + 1) % 7;

    private void Set(
        SubjectGroup group,
        Facility facility,
        Guid? staffId,
        int dayOfWeek,
        TimeOnly start,
        TimeOnly end,
        Recurrence recurrence,
        DateOnly validFrom,
        DateOnly? validUntil,
        Term term)
    {
        if (dayOfWeek is < 0 or > 6)
            throw new ArgumentException("Between 0 and 6.", nameof(dayOfWeek));

        if (end <= start)
            throw new ArgumentException("Must be after the start time.", "endTime");

        if (!term.Contains(validFrom))
            throw new ArgumentException("Must be inside the term.", nameof(validFrom));

        if (recurrence == Recurrence.Once && ProductDay(validFrom) != dayOfWeek)
            throw new ArgumentException("A one-off slot must fall on its day of the week.", nameof(validFrom));

        var effectiveValidUntil = validUntil ?? (recurrence == Recurrence.Once ? validFrom : term.EndsOn);

        if (!term.Contains(effectiveValidUntil))
            throw new ArgumentException("Must be inside the term.", nameof(validUntil));

        if (effectiveValidUntil < validFrom)
            throw new ArgumentException("Must be on or after the start date.", nameof(validUntil));

        if (recurrence == Recurrence.Once && effectiveValidUntil != validFrom)
            throw new ArgumentException("A one-off slot lasts a single day.", nameof(validUntil));

        if (!Enum.IsDefined(recurrence))
            throw new ArgumentException("Unknown recurrence.", nameof(recurrence));

        SubjectGroupId = group.Id;
        FacilityId = facility.Id;
        StaffId = staffId;
        DayOfWeek = dayOfWeek;
        StartTime = start;
        EndTime = end;
        Recurrence = recurrence;
        ValidFrom = validFrom;
        ValidUntil = effectiveValidUntil;
    }

    private DateOnly FirstOccurrenceOnOrAfter(DateOnly date)
    {
        var offset = (DayOfWeek - ProductDay(date) + 7) % 7;
        return date.AddDays(offset);
    }
}
