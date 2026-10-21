// ACCEPTANCE TEST — written by the consultant for phase P05-payments. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Payments.Domain.Ledger;
using NIMI.UniPortal.Modules.Payments.Domain.Online;

namespace NIMI.UniPortal.Modules.UnitTests.Payments;

/// <summary>Payment intents (online checkout, PAYGW) and receipt numbers (rule R3).</summary>
public sealed class OnlinePaymentDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_intent_is_pending_for_30_minutes()
    {
        var intent = NewIntent();

        Assert.Equal(7, intent.Id.Version);
        Assert.Equal(1000m, intent.Amount);
        Assert.Equal("mock", intent.Provider);
        Assert.Equal(PaymentIntentStatus.Pending, intent.Status);
        Assert.Equal(Now.AddMinutes(30), intent.ExpiresAt);
        Assert.Equal(PaymentIntentStatus.Pending, intent.StatusAt(Now.AddMinutes(29)));
        Assert.Equal(PaymentIntentStatus.Expired, intent.StatusAt(Now.AddMinutes(30)));
    }

    [Fact]
    public void An_intent_succeeds_or_fails_exactly_once()
    {
        var succeeded = NewIntent();
        var failed = NewIntent();
        succeeded.AttachProviderReference("mock_1");

        succeeded.Succeed(Now.AddMinutes(1));
        failed.Fail("Card declined", Now.AddMinutes(1));

        Assert.Equal("mock_1", succeeded.ProviderReference);
        Assert.Equal(PaymentIntentStatus.Succeeded, succeeded.Status);
        Assert.Equal(Now.AddMinutes(1), succeeded.CompletedAt);
        Assert.Equal(PaymentIntentStatus.Failed, failed.Status);
        Assert.Equal("Card declined", failed.FailureReason);
        Assert.Throws<InvalidOperationException>(() => succeeded.Succeed(Now.AddMinutes(2)));
        Assert.Throws<InvalidOperationException>(() => succeeded.Fail("late", Now.AddMinutes(2)));
        Assert.Throws<InvalidOperationException>(() => failed.Succeed(Now.AddMinutes(2)));
    }

    [Fact]
    public void An_expired_intent_cannot_succeed()
    {
        var intent = NewIntent();

        Assert.Throws<InvalidOperationException>(() => intent.Succeed(Now.AddMinutes(31)));
        Assert.Equal(PaymentIntentStatus.Expired, intent.StatusAt(Now.AddMinutes(31)));
    }

    [Fact]
    public void Intent_amounts_must_be_positive()
    {
        var error = Assert.ThrowsAny<ArgumentException>(() =>
            PaymentIntent.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 0m, "mock", Now));

        Assert.Equal("amount", error.ParamName);
    }

    [Theory]
    [InlineData(2026, 1, "RCP-2026-000001")]
    [InlineData(2027, 42, "RCP-2027-000042")]
    [InlineData(2026, 123456, "RCP-2026-123456")]
    public void Receipt_numbers_are_formatted_per_year(int year, int sequence, string expected)
    {
        Assert.Equal(expected, ReceiptNumber.Format(year, sequence));
    }

    [Fact]
    public void Receipt_sequences_start_at_one()
    {
        Assert.Equal("sequence", Assert.ThrowsAny<ArgumentException>(() => ReceiptNumber.Format(2026, 0)).ParamName);
    }

    private static PaymentIntent NewIntent() =>
        PaymentIntent.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 1000m, "mock", Now);
}
