namespace NIMI.UniPortal.Modules.Library.Domain;

/// <summary>A place in a book's queue: waiting, ready (a copy is held), then fulfilled, cancelled or expired.</summary>
public enum ReservationStatus
{
    Waiting,
    Ready,
    Fulfilled,
    Cancelled,
    Expired,
}
