namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>A member's place in a book's queue (first come, first served); a ready hold lasts the role's hold days.</summary>
public sealed class Reservation
{
    private Reservation()
    {
    }

    public Guid Id { get; private set; }

    public Guid BookId { get; private set; }

    /// <summary>Users user id (cross-schema id, no FK).</summary>
    public Guid UserId { get; private set; }

    public ReservationStatus Status { get; private set; }

    public Guid? CopyId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadyAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public bool IsActive => Status is ReservationStatus.Waiting or ReservationStatus.Ready;

    public static Reservation Place(Guid bookId, Guid userId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            BookId = bookId,
            UserId = userId,
            Status = ReservationStatus.Waiting,
            CreatedAt = now,
        };

    public void MarkReady(Guid copyId, DateTimeOffset now, int holdDays)
    {
        if (Status != ReservationStatus.Waiting)
            throw new InvalidOperationException("Only a waiting reservation becomes ready.");

        Status = ReservationStatus.Ready;
        CopyId = copyId;
        ReadyAt = now;
        ExpiresAt = now.AddDays(holdDays);
    }

    public bool HasExpired(DateTimeOffset now) => Status == ReservationStatus.Ready && now > ExpiresAt;

    public void Fulfill(DateTimeOffset now) => Close(ReservationStatus.Ready, ReservationStatus.Fulfilled, now);

    public void Expire(DateTimeOffset now) => Close(ReservationStatus.Ready, ReservationStatus.Expired, now);

    public void Cancel(DateTimeOffset now)
    {
        if (!IsActive)
            throw new InvalidOperationException("This reservation is already closed.");

        Status = ReservationStatus.Cancelled;
        ClosedAt = now;
    }

    private void Close(ReservationStatus from, ReservationStatus to, DateTimeOffset now)
    {
        if (Status != from)
            throw new InvalidOperationException($"Only a {from.ToString().ToLowerInvariant()} reservation can become {to.ToString().ToLowerInvariant()}.");

        Status = to;
        ClosedAt = now;
    }
}
