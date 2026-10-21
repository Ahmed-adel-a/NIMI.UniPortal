namespace NIMI.UniPortal.Modules.Payments.Domain.Online;

/// <summary>
/// One online checkout attempt (PAYGW, rules R9–R11). It is pending for 30 minutes, can complete at most once,
/// and its status for readers is computed from the clock — there is no background expiry job.
/// </summary>
public sealed class PaymentIntent
{
    private PaymentIntent()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid FeeItemId { get; private set; }

    public decimal Amount { get; private set; }

    /// <summary>The selected gateway name, lower-case (<c>mock</c>, <c>paymob</c>, …).</summary>
    public string Provider { get; private set; } = null!;

    /// <summary>The provider's own id for this checkout; null until checkout creation returns it.</summary>
    public string? ProviderReference { get; private set; }

    public PaymentIntentStatus Status { get; private set; }

    /// <summary>Safe, provider-supplied reason when <see cref="Status"/> is failed.</summary>
    public string? FailureReason { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Creates a pending intent; the amount must be positive with at most two decimals.</summary>
    public static PaymentIntent Create(Guid studentId, Guid feeItemId, decimal amount, string provider, DateTimeOffset now)
    {
        if (amount <= 0 || amount != decimal.Round(amount, 2))
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must be positive with at most 2 decimals.");

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Required.", nameof(provider));

        return new PaymentIntent
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            FeeItemId = feeItemId,
            Amount = amount,
            Provider = provider.Trim().ToLowerInvariant(),
            Status = PaymentIntentStatus.Pending,
            ExpiresAt = now.AddMinutes(30),
            CreatedAt = now,
        };
    }

    /// <summary>Stores the provider's checkout reference returned by <c>CreateCheckoutAsync</c>.</summary>
    public void AttachProviderReference(string providerReference)
    {
        if (string.IsNullOrWhiteSpace(providerReference))
            throw new ArgumentException("Required.", nameof(providerReference));

        var trimmed = providerReference.Trim();
        if (trimmed.Length > 100)
            throw new ArgumentException("Must be at most 100 characters.", nameof(providerReference));

        ProviderReference = trimmed;
    }

    /// <summary>Marks a pending, unexpired intent as succeeded; every other transition is invalid.</summary>
    public void Succeed(DateTimeOffset now)
    {
        if (Status != PaymentIntentStatus.Pending || now >= ExpiresAt)
            throw new InvalidOperationException("Only a pending, unexpired payment intent can succeed.");

        Status = PaymentIntentStatus.Succeeded;
        CompletedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Marks a pending intent as failed with a safe reason; repeated completion is invalid.</summary>
    public void Fail(string reason, DateTimeOffset now)
    {
        if (Status != PaymentIntentStatus.Pending)
            throw new InvalidOperationException("Only a pending payment intent can fail.");

        var trimmed = string.IsNullOrWhiteSpace(reason) ? "The payment failed." : reason.Trim();
        FailureReason = trimmed.Length <= 500 ? trimmed : trimmed[..500];
        Status = PaymentIntentStatus.Failed;
        CompletedAt = now;
        UpdatedAt = now;
    }

    /// <summary>Rule R11: pending intents become expired exactly at <see cref="ExpiresAt"/>.</summary>
    public PaymentIntentStatus StatusAt(DateTimeOffset now) =>
        Status == PaymentIntentStatus.Pending && now >= ExpiresAt
            ? PaymentIntentStatus.Expired
            : Status;
}
