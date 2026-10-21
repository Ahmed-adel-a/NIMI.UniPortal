// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;
using static NIMI.UniPortal.Modules.UnitTests.Scheduling.SchedulingFixture;

namespace NIMI.UniPortal.Modules.UnitTests.Scheduling;

/// <summary>Rule R8 — the overlap / conflict matrix. Every check is asserted in both directions.</summary>
public sealed class ScheduleSlotConflictTests
{
    private readonly Term _term = NewTerm();
    private readonly Facility _hallA = NewHall("Hall A");
    private readonly Facility _hallB = NewHall("Hall B");
    private readonly SubjectGroup _group1;
    private readonly SubjectGroup _group2;

    public ScheduleSlotConflictTests()
    {
        _group1 = NewGroup(_term, NewSubject("CS301"));
        _group2 = NewGroup(_term, NewSubject("CS302"));
    }

    [Fact]
    public void Same_hall_at_an_overlapping_time_conflicts()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30");
        var b = NewSlot(_term, _group2, _hallA, null, Sunday, "10:00", "11:00");

        AssertConflict(a, b, true);
    }

    [Fact]
    public void Back_to_back_slots_do_not_overlap()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30");
        var b = NewSlot(_term, _group2, _hallA, null, Sunday, "10:30", "12:00");

        AssertOverlap(a, b, false);
        AssertConflict(a, b, false);
    }

    [Fact]
    public void Different_days_do_not_overlap()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30");
        var b = NewSlot(_term, _group2, _hallA, null, Thursday, "09:00", "10:30");

        AssertOverlap(a, b, false);
        AssertConflict(a, b, false);
    }

    [Fact]
    public void Same_time_without_a_shared_hall_group_or_staff_overlaps_but_does_not_conflict()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30");
        var b = NewSlot(_term, _group2, _hallB, null, Sunday, "09:00", "10:30");

        AssertOverlap(a, b, true);
        AssertConflict(a, b, false);
    }

    [Fact]
    public void Same_group_in_another_hall_conflicts()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30");
        var b = NewSlot(_term, _group1, _hallB, null, Sunday, "10:00", "11:00");

        AssertConflict(a, b, true);
    }

    [Fact]
    public void Same_staff_member_in_another_hall_for_another_group_conflicts()
    {
        var professor = Guid.CreateVersion7();
        var a = NewSlot(_term, _group1, _hallA, professor, Sunday, "09:00", "10:30");
        var b = NewSlot(_term, _group2, _hallB, professor, Sunday, "10:00", "11:00");

        AssertConflict(a, b, true);
    }

    [Fact]
    public void Different_staff_members_do_not_conflict()
    {
        var a = NewSlot(_term, _group1, _hallA, Guid.CreateVersion7(), Sunday, "09:00", "10:30");
        var b = NewSlot(_term, _group2, _hallB, Guid.CreateVersion7(), Sunday, "09:00", "10:30");

        AssertConflict(a, b, false);
    }

    [Fact]
    public void A_cancelled_slot_never_overlaps_or_conflicts()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30");
        var b = NewSlot(_term, _group2, _hallA, null, Sunday, "09:00", "10:30");

        b.Cancel(Now);

        AssertOverlap(a, b, false);
        AssertConflict(a, b, false);
    }

    [Fact]
    public void Date_ranges_that_do_not_meet_do_not_conflict()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30", validUntil: Date(2026, 10, 31));
        var b = NewSlot(_term, _group2, _hallA, null, Sunday, "09:00", "10:30", validFrom: Date(2026, 11, 1));

        AssertConflict(a, b, false);
    }

    [Fact]
    public void Date_ranges_that_meet_on_one_occurrence_conflict()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30", validUntil: Date(2026, 11, 1));
        var b = NewSlot(_term, _group2, _hallA, null, Sunday, "09:00", "10:30", validFrom: Date(2026, 11, 1));

        AssertConflict(a, b, true);
    }

    [Fact]
    public void Biweekly_slots_on_alternate_weeks_share_a_hall()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30", Recurrence.Biweekly, Date(2026, 9, 20));
        var b = NewSlot(_term, _group2, _hallA, null, Sunday, "09:00", "10:30", Recurrence.Biweekly, Date(2026, 9, 27));

        AssertConflict(a, b, false);
    }

    [Fact]
    public void Biweekly_slots_on_the_same_weeks_conflict()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30", Recurrence.Biweekly, Date(2026, 9, 20));
        var b = NewSlot(_term, _group2, _hallA, null, Sunday, "09:00", "10:30", Recurrence.Biweekly, Date(2026, 10, 4));

        AssertConflict(a, b, true);
    }

    [Fact]
    public void A_biweekly_slot_conflicts_with_a_weekly_one()
    {
        var a = NewSlot(_term, _group1, _hallA, null, Sunday, "09:00", "10:30", Recurrence.Biweekly, Date(2026, 9, 27));
        var b = NewSlot(_term, _group2, _hallA, null, Sunday, "09:00", "10:30");

        AssertConflict(a, b, true);
    }

    [Fact]
    public void A_once_slot_conflicts_only_with_slots_that_occur_on_its_date()
    {
        var once = NewSlot(_term, _group1, _hallA, null, Thursday, "12:00", "14:00", Recurrence.Once, Date(2026, 10, 8));
        var weekly = NewSlot(_term, _group2, _hallA, null, Thursday, "13:00", "15:00");
        var weeklyFromLater = NewSlot(_term, _group2, _hallA, null, Thursday, "13:00", "15:00", validFrom: Date(2026, 10, 9));
        var biweeklyOffWeek = NewSlot(_term, _group2, _hallA, null, Thursday, "13:00", "15:00", Recurrence.Biweekly, Date(2026, 10, 1));
        var biweeklyOnWeek = NewSlot(_term, _group2, _hallA, null, Thursday, "13:00", "15:00", Recurrence.Biweekly, Date(2026, 9, 24));

        AssertConflict(once, weekly, true);
        AssertConflict(once, weeklyFromLater, false);
        AssertConflict(once, biweeklyOffWeek, false);     // Oct 1, 15, 29 …
        AssertConflict(once, biweeklyOnWeek, true);       // Sep 24, Oct 8 …
    }

    private static void AssertOverlap(ScheduleSlot a, ScheduleSlot b, bool expected)
    {
        Assert.Equal(expected, a.OverlapsInTime(b));
        Assert.Equal(expected, b.OverlapsInTime(a));
    }

    private static void AssertConflict(ScheduleSlot a, ScheduleSlot b, bool expected)
    {
        Assert.Equal(expected, a.ConflictsWith(b));
        Assert.Equal(expected, b.ConflictsWith(a));
    }
}
