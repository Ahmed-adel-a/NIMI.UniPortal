using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Infrastructure.Persistence;

namespace NIMI.UniPortal.Modules.Events.Infrastructure.Seats;

/// <summary>
/// Rule R2 in SQL, the P4 pattern: <c>INSERT … ON CONFLICT</c> creates/refreshes the row with the event's capacity, then
/// <c>UPDATE … SET taken = taken + 1 WHERE taken &lt; capacity</c> decides a race in one statement — concurrent
/// registrants serialize on the row lock and at most <c>capacity</c> of them ever succeed.
/// </summary>
internal sealed class EventSeats(EventsDbContext db) : IEventSeats
{
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is null)
            await db.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task<bool> TryTakeAsync(Guid eventId, int capacity, CancellationToken cancellationToken = default)
    {
        // GREATEST keeps taken <= capacity if Student Affairs lowered the capacity (never below seats taken).
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO events.event_seats AS es (event_id, capacity, taken)
            VALUES ({eventId}, {capacity}, 0)
            ON CONFLICT (event_id) DO UPDATE
            SET capacity = GREATEST(EXCLUDED.capacity, es.taken)
            """, cancellationToken);

        var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE events.event_seats
            SET taken = taken + 1
            WHERE event_id = {eventId} AND taken < capacity
            """, cancellationToken);

        return updated > 0;
    }

    public async Task ReleaseAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE events.event_seats
            SET taken = GREATEST(taken - 1, 0)
            WHERE event_id = {eventId}
            """, cancellationToken);

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is not null)
            await db.Database.RollbackTransactionAsync(cancellationToken);
    }
}
