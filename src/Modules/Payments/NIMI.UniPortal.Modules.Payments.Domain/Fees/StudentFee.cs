namespace NIMI.UniPortal.Modules.Payments.Domain.Fees;

/// <summary>
/// What one student owes against one fee item (P5). The amount due is a snapshot of the fee item at creation;
/// later price changes never move it. Payments update <see cref="AmountPaid"/> and the derived status (rule R4).
/// </summary>
public sealed class StudentFee
{
    private StudentFee()
    {
    }

    public Guid Id { get; private set; }

    public Guid FeeItemId { get; private set; }

    public Guid StudentId { get; private set; }

    public decimal AmountDue { get; private set; }

    public decimal AmountPaid { get; private set; }

    public decimal Balance => AmountDue - AmountPaid;

    public StudentFeeStatus Status { get; private set; }

    public bool IsFullyPaid => AmountPaid == AmountDue;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Creates a pending student fee; the item's amount is copied, never referenced later.</summary>
    public static StudentFee Create(FeeItem item, Guid studentId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            FeeItemId = item.Id,
            StudentId = studentId,
            AmountDue = item.Amount,
            AmountPaid = 0m,
            Status = StudentFeeStatus.Pending,
            CreatedAt = now,
        };

    /// <summary>
    /// Applies a payment to this fee (rule R2). Rejects zero/negative amounts, more than two decimals, and anything
    /// above the outstanding balance — the caller maps the argument name to the HTTP field <c>amount</c>.
    /// </summary>
    public void ApplyPayment(decimal amount, DateTimeOffset now)
    {
        if (amount <= 0 || amount != decimal.Round(amount, 2))
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Must be positive with at most 2 decimals.");

        if (amount > Balance)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Cannot exceed the outstanding balance.");

        AmountPaid += amount;
        Status = AmountPaid == AmountDue
            ? StudentFeeStatus.Paid
            : AmountPaid > 0
                ? StudentFeeStatus.PartiallyPaid
                : StudentFeeStatus.Pending;
        UpdatedAt = now;
    }
}
