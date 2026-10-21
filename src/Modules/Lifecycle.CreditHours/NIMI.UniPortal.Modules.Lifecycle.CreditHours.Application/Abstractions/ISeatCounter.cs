namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;

/// <summary>
/// The atomic seat counter of a subject group (rule R8). The implementation owns the SQL and the transaction
/// boundary: register/drop call <see cref="BeginAsync"/>, the conditional UPDATE decides a race, and the DbContext
/// commits the transaction together with the command's single save.
/// </summary>
public interface ISeatCounter
{
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>Refreshes the capacity and takes one seat; <c>false</c> means the group is full.</summary>
    Task<bool> TryTakeAsync(Guid subjectGroupId, int capacity, CancellationToken cancellationToken = default);

    /// <summary>Gives a seat back after a drop (never below zero).</summary>
    Task ReleaseAsync(Guid subjectGroupId, CancellationToken cancellationToken = default);

    /// <summary>Rolls back the open transaction; a no-op when there is none.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
