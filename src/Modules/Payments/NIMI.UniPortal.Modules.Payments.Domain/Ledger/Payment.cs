namespace NIMI.UniPortal.Modules.Payments.Domain.Ledger;

/// <summary>
/// One money movement against a student fee (P5.3). Rows are append-only: a refund or correction is a new decision,
/// never an edit of this row. Online payments also point at their <c>payment_intents</c> row (PAYGW).
/// </summary>
public sealed class Payment
{
    private Payment()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentFeeId { get; private set; }

    public Guid StudentId { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    /// <summary>Office reference (bank transfer number, receipt …); null for cash and online callbacks.</summary>
    public string? Reference { get; private set; }

    public Guid? PaymentIntentId { get; private set; }

    public string ReceiptNumber { get; private set; } = null!;

    public Guid RecordedBy { get; private set; }

    public DateTimeOffset PaidAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates an append-only payment row; the caller has already applied it to the student fee.</summary>
    public static Payment Create(
        Guid studentFeeId,
        Guid studentId,
        decimal amount,
        PaymentMethod method,
        string? reference,
        Guid? paymentIntentId,
        string receiptNumber,
        Guid recordedBy,
        DateTimeOffset paidAt,
        DateTimeOffset now)
    {
        if (amount <= 0 || amount != decimal.Round(amount, 2))
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must be positive with at most 2 decimals.");

        if (!Enum.IsDefined(method))
            throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown payment method.");

        return new Payment
        {
            Id = Guid.CreateVersion7(),
            StudentFeeId = studentFeeId,
            StudentId = studentId,
            Amount = amount,
            Method = method,
            Reference = Optional(reference, nameof(reference), 100),
            PaymentIntentId = paymentIntentId,
            ReceiptNumber = Required(receiptNumber, nameof(receiptNumber), 30),
            RecordedBy = recordedBy,
            PaidAt = paidAt,
            CreatedAt = now,
        };
    }

    private static string Required(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Required.", field);

        if (trimmed.Length > maxLength)
            throw new ArgumentException($"Must be at most {maxLength} characters.", field);

        return trimmed;
    }

    private static string? Optional(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"Must be at most {maxLength} characters.", field);

        return trimmed;
    }
}
