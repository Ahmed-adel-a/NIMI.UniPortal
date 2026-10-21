// ACCEPTANCE TEST — written by the consultant for phase P03. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using static NIMI.UniPortal.Modules.UnitTests.Scheduling.SchedulingFixture;

namespace NIMI.UniPortal.Modules.UnitTests.Scheduling;

public sealed class TermTests
{
    [Fact]
    public void A_new_term_is_planned()
    {
        var term = NewTerm();

        Assert.Equal(7, term.Id.Version);
        Assert.Equal(2026, term.AcademicYear);
        Assert.Equal(TermNumber.First, term.Number);
        Assert.Equal(TermStart, term.StartsOn);
        Assert.Equal(TermEnd, term.EndsOn);
        Assert.Equal(17, term.WeekCount);
        Assert.Equal(TermStatus.Planned, term.Status);
    }

    [Fact]
    public void Invalid_values_are_rejected_with_their_field_name()
    {
        AssertRejected("endsOn", () => Term.Create(2026, TermNumber.First, TermStart, TermStart, 17, Now));
        AssertRejected("endsOn", () => Term.Create(2026, TermNumber.First, TermStart, TermStart.AddDays(-1), 17, Now));
        AssertRejected("weekCount", () => Term.Create(2026, TermNumber.First, TermStart, TermEnd, 0, Now));
        AssertRejected("weekCount", () => Term.Create(2026, TermNumber.First, TermStart, TermEnd, 31, Now));
        AssertRejected("academicYear", () => Term.Create(1999, TermNumber.First, TermStart, TermEnd, 17, Now));
    }

    [Fact]
    public void A_term_goes_planned_then_active_then_closed()
    {
        var term = NewTerm();

        term.Activate(Now);
        Assert.Equal(TermStatus.Active, term.Status);

        term.Close(Now);
        Assert.Equal(TermStatus.Closed, term.Status);
    }

    [Fact]
    public void A_planned_term_can_be_edited()
    {
        var term = NewTerm();

        term.Update(2026, TermNumber.Second, Date(2027, 2, 6), Date(2027, 6, 4), 16, Now);

        Assert.Equal(TermNumber.Second, term.Number);
        Assert.Equal(Date(2027, 2, 6), term.StartsOn);
        Assert.Equal(16, term.WeekCount);
    }

    [Fact]
    public void An_active_or_closed_term_cannot_be_edited_and_a_closed_one_cannot_be_reactivated()
    {
        var term = NewTerm();
        term.Activate(Now);

        Assert.Throws<InvalidOperationException>(() => term.Update(2026, TermNumber.First, TermStart, TermEnd, 16, Now));

        term.Close(Now);
        Assert.Throws<InvalidOperationException>(() => term.Update(2026, TermNumber.First, TermStart, TermEnd, 16, Now));
        Assert.Throws<InvalidOperationException>(() => term.Activate(Now));
    }

    [Fact]
    public void Only_an_active_term_can_be_closed()
    {
        Assert.Throws<InvalidOperationException>(() => NewTerm().Close(Now));
    }

    [Fact]
    public void Contains_includes_both_ends()
    {
        var term = NewTerm();

        Assert.True(term.Contains(TermStart));
        Assert.True(term.Contains(TermEnd));
        Assert.False(term.Contains(TermStart.AddDays(-1)));
        Assert.False(term.Contains(TermEnd.AddDays(1)));
    }

    private static void AssertRejected(string field, Action act) =>
        Assert.Equal(field, Assert.ThrowsAny<ArgumentException>(act).ParamName);
}
