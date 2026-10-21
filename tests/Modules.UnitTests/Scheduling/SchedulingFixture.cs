// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using System.Globalization;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;

namespace NIMI.UniPortal.Modules.UnitTests.Scheduling;

/// <summary>
/// Builds valid scheduling objects for the domain tests. Calendar: the term runs Saturday 2026-09-19 →
/// Friday 2027-01-15 (17 weeks). Product days: 0 = Saturday, 1 = Sunday … 5 = Thursday, 6 = Friday.
/// </summary>
internal static class SchedulingFixture
{
    public static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly TermStart = new(2026, 9, 19);   // Saturday
    public static readonly DateOnly TermEnd = new(2027, 1, 15);     // Friday
    public const int Sunday = 1;
    public const int Thursday = 5;

    public static Term NewTerm() => Term.Create(2026, TermNumber.First, TermStart, TermEnd, 17, Now);

    public static Subject NewSubject(string code = "CS301") =>
        Subject.Create(Guid.CreateVersion7(), code, "Algorithms", "الخوارزميات", null, 3, SubjectCategory.Regular, false, Now);

    public static SubjectGroup NewGroup(Term term, Subject? subject = null, int number = 1) =>
        SubjectGroup.Create(subject ?? NewSubject(), term, number, null, 60, Now);

    public static Facility NewHall(string name = "Hall A") =>
        Facility.Create(null, name, FacilityType.LectureHall, 120, "Main building", "1", Now);

    public static ScheduleSlot NewSlot(
        Term term,
        SubjectGroup group,
        Facility hall,
        Guid? staffId,
        int dayOfWeek,
        string start,
        string end,
        Recurrence recurrence = Recurrence.Weekly,
        DateOnly? validFrom = null,
        DateOnly? validUntil = null) =>
        ScheduleSlot.Create(
            group, hall, staffId, dayOfWeek, Time(start), Time(end), recurrence,
            validFrom ?? TermStart, validUntil, term, Guid.CreateVersion7(), Now);

    public static TimeOnly Time(string hhmm) => TimeOnly.ParseExact(hhmm, "HH:mm", CultureInfo.InvariantCulture);

    public static DateOnly Date(int year, int month, int day) => new(year, month, day);
}
