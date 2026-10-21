// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;
using static NIMI.UniPortal.Modules.UnitTests.Scheduling.SchedulingFixture;

namespace NIMI.UniPortal.Modules.UnitTests.Scheduling;

/// <summary>Which calendar dates a slot actually takes place on (used by teaching/student schedules and attendance).</summary>
public sealed class ScheduleOccurrenceTests
{
    [Fact]
    public void Product_day_numbers_map_to_real_calendar_days()
    {
        Assert.Equal(DayOfWeek.Saturday, TermStart.DayOfWeek);                 // day 0
        Assert.Equal(DayOfWeek.Sunday, Date(2026, 9, 20).DayOfWeek);           // day 1
        Assert.Equal(DayOfWeek.Thursday, Date(2026, 9, 24).DayOfWeek);         // day 5
    }

    [Fact]
    public void A_weekly_slot_occurs_every_week_on_its_day()
    {
        var slot = Slot(Recurrence.Weekly, Sunday);

        Assert.Equal([Date(2026, 9, 20), Date(2026, 9, 27), Date(2026, 10, 4)], slot.OccurrencesBetween(TermStart, Date(2026, 10, 9)));
    }

    [Fact]
    public void The_first_occurrence_is_the_first_matching_day_on_or_after_valid_from()
    {
        var slot = Slot(Recurrence.Weekly, Sunday, validFrom: Date(2026, 9, 22));   // a Tuesday

        Assert.Equal(Date(2026, 9, 27), slot.OccurrencesBetween(TermStart, TermEnd)[0]);
    }

    [Fact]
    public void A_biweekly_slot_occurs_every_other_week_from_its_first_occurrence()
    {
        var slot = Slot(Recurrence.Biweekly, Sunday, validFrom: Date(2026, 9, 20));

        Assert.Equal([Date(2026, 9, 20), Date(2026, 10, 4), Date(2026, 10, 18)], slot.OccurrencesBetween(TermStart, Date(2026, 10, 31)));
    }

    [Fact]
    public void A_once_slot_occurs_only_on_its_date()
    {
        var slot = Slot(Recurrence.Once, Thursday, validFrom: Date(2026, 9, 24));

        Assert.Equal([Date(2026, 9, 24)], slot.OccurrencesBetween(TermStart, TermEnd));
        Assert.Empty(slot.OccurrencesBetween(Date(2026, 9, 25), TermEnd));
    }

    [Fact]
    public void Occurrences_stop_at_valid_until_and_at_the_end_of_the_term()
    {
        var shortSlot = Slot(Recurrence.Weekly, Sunday, validUntil: Date(2026, 10, 4));
        var fullTerm = Slot(Recurrence.Weekly, Sunday);

        Assert.Equal([Date(2026, 9, 20), Date(2026, 9, 27), Date(2026, 10, 4)], shortSlot.OccurrencesBetween(TermStart, Date(2026, 12, 31)));
        Assert.Equal([Date(2027, 1, 3), Date(2027, 1, 10)], fullTerm.OccurrencesBetween(Date(2027, 1, 1), Date(2027, 1, 31)));
    }

    [Fact]
    public void The_range_bounds_are_inclusive()
    {
        var slot = Slot(Recurrence.Weekly, Sunday);

        Assert.Equal([Date(2026, 9, 20)], slot.OccurrencesBetween(Date(2026, 9, 20), Date(2026, 9, 20)));
    }

    [Fact]
    public void A_cancelled_slot_has_no_occurrences()
    {
        var slot = Slot(Recurrence.Weekly, Sunday);

        slot.Cancel(Now);

        Assert.Empty(slot.OccurrencesBetween(TermStart, TermEnd));
    }

    private static ScheduleSlot Slot(Recurrence recurrence, int day, DateOnly? validFrom = null, DateOnly? validUntil = null)
    {
        var term = NewTerm();
        return NewSlot(term, NewGroup(term), NewHall(), null, day, "09:00", "10:30", recurrence, validFrom, validUntil);
    }
}
