using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Persistence;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Infrastructure.Seats;

/// <summary>
/// Rule R8 in SQL. <c>INSERT … ON CONFLICT</c> creates/refreshes the row (capacity from Scheduling);
/// <c>UPDATE … SET taken = taken + 1 WHERE taken &lt; capacity</c> is one atomic statement, so concurrent racers
/// serialize on the row lock and at most <c>capacity</c> of them ever see one row affected. The loser rolls back
/// and the API answers 409 "The group is full." — never over-booked.
/// </summary>
internal sealed class SeatCounter(CreditHoursDbContext db) : ISeatCounter
{
    public async Task BeginAsync(CancellationToken cancellationToken = default) =>
        await db.Database.BeginTransactionAsync(cancellationToken);

    public async Task<bool> TryTakeAsync(
        Guid subjectGroupId, int capacity, CancellationToken cancellationToken = default)
    {
        // GREATEST keeps the taken <= capacity invariant if an admin lowered the capacity below seats already taken.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO lifecycle_credit.group_seats AS gs (subject_group_id, capacity, taken)
            VALUES ({subjectGroupId}, {capacity}, 0)
            ON CONFLICT (subject_group_id) DO UPDATE
            SET capacity = GREATEST(EXCLUDED.capacity, gs.taken)
            """, cancellationToken);

        var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE lifecycle_credit.group_seats
            SET taken = taken + 1
            WHERE subject_group_id = {subjectGroupId} AND taken < capacity
            """, cancellationToken);

        return updated > 0;
    }

    public async Task ReleaseAsync(Guid subjectGroupId, CancellationToken cancellationToken = default) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE lifecycle_credit.group_seats
            SET taken = GREATEST(taken - 1, 0)
            WHERE subject_group_id = {subjectGroupId}
            """, cancellationToken);

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is not null)
            await db.Database.RollbackTransactionAsync(cancellationToken);
    }
}
