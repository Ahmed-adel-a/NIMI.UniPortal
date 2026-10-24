using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using static NIMI.UniPortal.Modules.UnitTests.Scheduling.SchedulingFixture;

namespace NIMI.UniPortal.Modules.UnitTests.Scheduling;

/// <summary>Copying a term keeps each timetable date at the same distance from the term's start (week 3 stays week 3).</summary>
public sealed class TermShiftTests
{
    // 2027/2028 first term: Saturday 2027-09-18 → Friday 2028-01-14, the same 17 weeks one year later.
    private static Term NextYear(DateOnly? endsOn = null) =>
        Term.Create(2027, TermNumber.First, new DateOnly(2027, 9, 18), endsOn ?? new DateOnly(2028, 1, 14), 17, Now);

    [Fact]
    public void The_start_of_one_term_is_the_start_of_the_other()
    {
        Assert.Equal(new DateOnly(2027, 9, 18), NextYear().ShiftFrom(NewTerm(), TermStart));
    }

    [Fact]
    public void A_date_keeps_its_distance_from_the_start()
    {
        var shifted = NextYear().ShiftFrom(NewTerm(), new DateOnly(2026, 10, 3)); // 14 days in: week 3

        Assert.Equal(new DateOnly(2027, 10, 2), shifted);
        Assert.Equal(DayOfWeek.Saturday, shifted!.Value.DayOfWeek);
    }

    [Fact]
    public void The_end_of_a_term_of_the_same_length_is_the_end_of_the_other()
    {
        Assert.Equal(new DateOnly(2028, 1, 14), NextYear().ShiftFrom(NewTerm(), TermEnd));
    }

    [Fact]
    public void A_date_after_the_end_of_a_shorter_term_has_no_equivalent()
    {
        var shorter = NextYear(endsOn: new DateOnly(2027, 12, 17)); // 13 weeks

        Assert.Null(shorter.ShiftFrom(NewTerm(), TermEnd));
        Assert.Equal(new DateOnly(2027, 12, 17), shorter.ShiftFrom(NewTerm(), new DateOnly(2026, 12, 18)));
    }
}
