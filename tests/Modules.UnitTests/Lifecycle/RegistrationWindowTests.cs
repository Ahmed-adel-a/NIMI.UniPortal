// ACCEPTANCE TEST — written by the consultant for phase P04A. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.UnitTests.Lifecycle;

public sealed class RegistrationWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Opens = Now.AddDays(1);
    private static readonly DateTimeOffset Closes = Now.AddDays(15);
    private static readonly Guid Term = Guid.CreateVersion7();

    [Fact]
    public void A_window_keeps_its_term_dates_and_credit_limits()
    {
        var window = RegistrationWindow.Create(Term, Opens, Closes, 12, 21, Now);

        Assert.Equal(7, window.Id.Version);
        Assert.Equal(Term, window.TermId);
        Assert.Equal(Opens, window.OpensAt);
        Assert.Equal(Closes, window.ClosesAt);
        Assert.Equal(12, window.MinCredits);
        Assert.Equal(21, window.MaxCredits);
    }

    [Fact]
    public void A_window_is_open_from_its_opening_instant_until_just_before_it_closes()
    {
        var window = RegistrationWindow.Create(Term, Opens, Closes, 12, 21, Now);

        Assert.False(window.IsOpen(Opens.AddTicks(-1)));
        Assert.True(window.IsOpen(Opens));
        Assert.True(window.IsOpen(Closes.AddTicks(-1)));
        Assert.False(window.IsOpen(Closes));
    }

    [Fact]
    public void Invalid_windows_are_rejected_with_their_field_name()
    {
        AssertRejected("closesAt", () => RegistrationWindow.Create(Term, Opens, Opens, 12, 21, Now));
        AssertRejected("closesAt", () => RegistrationWindow.Create(Term, Opens, Opens.AddDays(-1), 12, 21, Now));
        AssertRejected("minCredits", () => RegistrationWindow.Create(Term, Opens, Closes, -1, 21, Now));
        AssertRejected("maxCredits", () => RegistrationWindow.Create(Term, Opens, Closes, 12, 11, Now));
        AssertRejected("maxCredits", () => RegistrationWindow.Create(Term, Opens, Closes, 12, 41, Now));
    }

    [Fact]
    public void Updating_a_window_applies_the_same_rules()
    {
        var window = RegistrationWindow.Create(Term, Opens, Closes, 12, 21, Now);

        window.Update(Opens, Closes.AddDays(7), 9, 18, Now);

        Assert.Equal(Closes.AddDays(7), window.ClosesAt);
        Assert.Equal(9, window.MinCredits);
        Assert.Equal(18, window.MaxCredits);
        AssertRejected("maxCredits", () => window.Update(Opens, Closes, 12, 8, Now));
    }

    private static void AssertRejected(string field, Action act) =>
        Assert.Equal(field, Assert.ThrowsAny<ArgumentException>(act).ParamName);
}
