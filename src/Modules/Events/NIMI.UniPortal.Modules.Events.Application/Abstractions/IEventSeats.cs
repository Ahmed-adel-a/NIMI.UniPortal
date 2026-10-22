namespace NIMI.UniPortal.Modules.Events.Application.Abstractions;

/// <summary>
/// The atomic seat counter of an event with a capacity (rule R2, same pattern as P4 group seats). Register/cancel call
/// <see cref="BeginAsync"/>; the conditional UPDATE decides a race; the DbContext commits the transaction with the save.
/// </summary>
public interface IEventSeats
{
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>Refreshes the capacity and takes one seat; <c>false</c> means the event is full.</summary>
    Task<bool> TryTakeAsync(Guid eventId, int capacity, CancellationToken cancellationToken = default);

    /// <summary>Gives a seat back (never below zero).</summary>
    Task ReleaseAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Rolls back the open transaction; a no-op when there is none.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
