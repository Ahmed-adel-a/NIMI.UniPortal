// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Events;

/// <summary>P11 Events domain: schedule rules, the draft → published → cancelled flow, registration windows, check-in.</summary>
public sealed class CampusEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Start = Now.AddDays(7);
    private static readonly Guid Admin = Guid.CreateVersion7();

    [Fact]
    public void A_new_event_is_a_draft_with_trimmed_texts()
    {
        var e = Draft();

        Assert.Equal(EventStatus.Draft, e.Status);
        Assert.Equal("Career fair", e.Title);
        Assert.Equal("Main hall", e.Venue);
        Assert.False(e.IsOpenForRegistration(Now));
    }

    [Fact]
    public void The_schedule_must_make_sense()
    {
        Assert.Throws<ArgumentException>(() => CampusEvent.Create("t", "d", null, "v", Start, Start, null, Start.AddDays(-1), Admin, Now));
        Assert.Throws<ArgumentException>(() => CampusEvent.Create("t", "d", null, "v", Start, Start.AddHours(1), null, Start.AddMinutes(1), Admin, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => CampusEvent.Create("t", "d", null, "v", Start, Start.AddHours(1), 0, Start.AddDays(-1), Admin, Now));
        Assert.Throws<ArgumentException>(() => CampusEvent.Create(" ", "d", null, "v", Start, Start.AddHours(1), null, Start.AddDays(-1), Admin, Now));
        Assert.Throws<ArgumentException>(() => CampusEvent.Create("t", "d", null, " ", Start, Start.AddHours(1), null, Start.AddDays(-1), Admin, Now));
    }

    [Fact]
    public void Registration_is_open_only_while_published_and_before_the_deadline()
    {
        var e = Draft();
        e.Publish(Now);

        Assert.True(e.IsOpenForRegistration(Now));
        Assert.True(e.IsOpenForRegistration(Start.AddDays(-1)));
        Assert.False(e.IsOpenForRegistration(Start.AddDays(-1).AddSeconds(1)));
    }

    [Fact]
    public void Publishing_and_cancelling_move_forward_only()
    {
        var e = Draft();
        e.Publish(Now);
        Assert.Throws<InvalidOperationException>(() => e.Publish(Now));

        e.Cancel("Venue unavailable", Now);
        Assert.Equal(EventStatus.Cancelled, e.Status);
        Assert.Equal("Venue unavailable", e.CancellationReason);
        Assert.False(e.IsOpenForRegistration(Now));
        Assert.Throws<InvalidOperationException>(() => e.Cancel("again", Now));
        Assert.Throws<InvalidOperationException>(() => e.Update("t", "d", null, "v", Start, Start.AddHours(1), null, Start.AddDays(-1), Now));
    }

    [Fact]
    public void An_update_reports_whether_registrants_must_hear_about_it()
    {
        var e = Draft();
        e.Publish(Now);

        Assert.False(e.Update("New title", "New text", null, "Main hall", Start, Start.AddHours(2), null, Start.AddDays(-1), Now));
        Assert.True(e.Update("New title", "New text", null, "Hall 7", Start, Start.AddHours(2), null, Start.AddDays(-1), Now));
        Assert.True(e.Update("New title", "New text", null, "Hall 7", Start.AddHours(1), Start.AddHours(3), null, Start.AddDays(-1), Now));
    }

    [Fact]
    public void Check_in_runs_from_an_hour_before_the_start_to_the_end()
    {
        var e = Draft();

        Assert.False(e.IsCheckInOpen(Start.AddMinutes(-61)));
        Assert.True(e.IsCheckInOpen(Start.AddMinutes(-60)));
        Assert.True(e.IsCheckInOpen(Start.AddHours(2)));
        Assert.False(e.IsCheckInOpen(Start.AddHours(2).AddSeconds(1)));
    }

    [Fact]
    public void A_registration_is_cancelled_or_attended_once_and_can_be_renewed()
    {
        var registration = EventRegistration.Register(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);
        Assert.Equal(EventRegistrationStatus.Registered, registration.Status);

        registration.Cancel(Now.AddHours(1));
        Assert.Equal(EventRegistrationStatus.Cancelled, registration.Status);
        Assert.Throws<InvalidOperationException>(() => registration.CheckIn(Now));

        registration.Renew(Now.AddHours(2));
        Assert.Equal(EventRegistrationStatus.Registered, registration.Status);
        registration.CheckIn(Now.AddDays(7));
        Assert.Equal(EventRegistrationStatus.Attended, registration.Status);
        Assert.Throws<InvalidOperationException>(() => registration.CheckIn(Now.AddDays(7)));
    }

    private static CampusEvent Draft() =>
        CampusEvent.Create(" Career fair ", "Meet 40 companies.", null, " Main hall ", Start, Start.AddHours(2), 50, Start.AddDays(-1), Admin, Now);
}
