// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;
using static NIMI.UniPortal.Modules.UnitTests.Scheduling.SchedulingFixture;

namespace NIMI.UniPortal.Modules.UnitTests.Scheduling;

/// <summary>What makes a single timetable slot valid (rule R7).</summary>
public sealed class ScheduleSlotRulesTests
{
    [Fact]
    public void A_weekly_slot_without_an_end_date_runs_until_the_end_of_the_term()
    {
        var term = NewTerm();
        var group = NewGroup(term);
        var hall = NewHall();
        var staff = Guid.CreateVersion7();

        var slot = NewSlot(term, group, hall, staff, Sunday, "09:00", "10:30");

        Assert.Equal(7, slot.Id.Version);
        Assert.Equal(group.Id, slot.SubjectGroupId);
        Assert.Equal(hall.Id, slot.FacilityId);
        Assert.Equal(staff, slot.StaffId);
        Assert.Equal(Sunday, slot.DayOfWeek);
        Assert.Equal(Time("09:00"), slot.StartTime);
        Assert.Equal(Time("10:30"), slot.EndTime);
        Assert.Equal(Recurrence.Weekly, slot.Recurrence);
        Assert.Equal(TermStart, slot.ValidFrom);
        Assert.Equal(TermEnd, slot.ValidUntil);
        Assert.False(slot.IsCancelled);
    }

    [Fact]
    public void A_once_slot_ends_on_the_day_it_starts()
    {
        var term = NewTerm();

        var slot = NewSlot(term, NewGroup(term), NewHall(), null, Thursday, "12:00", "14:00", Recurrence.Once, Date(2026, 9, 24));

        Assert.Equal(Date(2026, 9, 24), slot.ValidUntil);
    }

    [Fact]
    public void Invalid_slots_are_rejected_with_their_field_name()
    {
        var term = NewTerm();
        var group = NewGroup(term);
        var hall = NewHall();

        AssertRejected("endTime", () => NewSlot(term, group, hall, null, Sunday, "10:30", "09:00"));
        AssertRejected("endTime", () => NewSlot(term, group, hall, null, Sunday, "09:00", "09:00"));
        AssertRejected("dayOfWeek", () => NewSlot(term, group, hall, null, 7, "09:00", "10:00"));
        AssertRejected("dayOfWeek", () => NewSlot(term, group, hall, null, -1, "09:00", "10:00"));
        AssertRejected("validFrom", () => NewSlot(term, group, hall, null, Sunday, "09:00", "10:00", validFrom: TermStart.AddDays(-1)));
        AssertRejected("validFrom", () => NewSlot(term, group, hall, null, Sunday, "09:00", "10:00", validFrom: TermEnd.AddDays(1)));
        AssertRejected("validUntil", () => NewSlot(term, group, hall, null, Sunday, "09:00", "10:00", validUntil: TermEnd.AddDays(1)));
        AssertRejected("validUntil", () => NewSlot(term, group, hall, null, Sunday, "09:00", "10:00",
            validFrom: Date(2026, 10, 1), validUntil: Date(2026, 9, 30)));
    }

    [Fact]
    public void A_once_slot_must_fall_on_its_day_and_last_a_single_day()
    {
        var term = NewTerm();
        var group = NewGroup(term);
        var hall = NewHall();

        AssertRejected("validFrom", () => NewSlot(term, group, hall, null, Sunday, "09:00", "10:00", Recurrence.Once, Date(2026, 9, 24)));   // a Thursday
        AssertRejected("validUntil", () => NewSlot(term, group, hall, null, Thursday, "09:00", "10:00", Recurrence.Once,
            Date(2026, 9, 24), Date(2026, 10, 1)));
    }

    [Fact]
    public void Cancelling_a_slot()
    {
        var term = NewTerm();
        var slot = NewSlot(term, NewGroup(term), NewHall(), null, Sunday, "09:00", "10:00");

        slot.Cancel(Now);

        Assert.True(slot.IsCancelled);
    }

    private static void AssertRejected(string field, Action act) =>
        Assert.Equal(field, Assert.ThrowsAny<ArgumentException>(act).ParamName);
}
