namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>One barcoded physical copy of a <see cref="Book"/>.</summary>
public sealed class BookCopy
{
    public const int MaxBarcodeLength = 50;
    public const int MaxShelfLength = 50;

    private BookCopy()
    {
    }

    public Guid Id { get; private set; }

    public Guid BookId { get; private set; }

    public string Barcode { get; private set; } = string.Empty;

    public string? Shelf { get; private set; }

    public CopyStatus Status { get; private set; }

    /// <summary>The ready reservation this copy waits for while <see cref="CopyStatus.OnHold"/>.</summary>
    public Guid? HeldForReservationId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static BookCopy Add(Guid bookId, string barcode, string? shelf, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            BookId = bookId,
            Barcode = LibraryText.Required(barcode, MaxBarcodeLength, nameof(barcode)),
            Shelf = LibraryText.Optional(shelf, MaxShelfLength, nameof(shelf)),
            Status = CopyStatus.Available,
            CreatedAt = now,
        };

    /// <summary>From the shelf, or from a hold (the caller checks the hold belongs to the borrower).</summary>
    public void Lend()
    {
        if (Status is not (CopyStatus.Available or CopyStatus.OnHold))
            throw new InvalidOperationException("This copy is not available.");

        Status = CopyStatus.OnLoan;
        HeldForReservationId = null;
    }

    public void ReturnToShelf()
    {
        if (Status is not (CopyStatus.OnLoan or CopyStatus.OnHold))
            throw new InvalidOperationException("Only a lent or held copy goes back to the shelf.");

        Status = CopyStatus.Available;
        HeldForReservationId = null;
    }

    public void HoldFor(Guid reservationId)
    {
        if (Status is CopyStatus.Lost or CopyStatus.Withdrawn)
            throw new InvalidOperationException("A lost or withdrawn copy can't be held.");

        Status = CopyStatus.OnHold;
        HeldForReservationId = reservationId;
    }

    /// <summary>Student Affairs marks a copy available, lost or withdrawn — never while it is lent or held.</summary>
    public void SetCondition(CopyStatus status)
    {
        if (Status is CopyStatus.OnLoan or CopyStatus.OnHold)
            throw new InvalidOperationException("Return the copy (or release its hold) first.");
        if (status is not (CopyStatus.Available or CopyStatus.Lost or CopyStatus.Withdrawn))
            throw new ArgumentOutOfRangeException(nameof(status), status, "Available, lost or withdrawn.");

        Status = status;
    }

    public void MoveTo(string? shelf) => Shelf = LibraryText.Optional(shelf, MaxShelfLength, nameof(shelf));
}
