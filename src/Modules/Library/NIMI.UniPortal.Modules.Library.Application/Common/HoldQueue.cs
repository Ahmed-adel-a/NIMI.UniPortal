using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>
/// A copy coming free (returned, newly added, or its hold ended) goes to the first member still waiting for its book —
/// held for their role's hold days and notified — or to the shelf when nobody waits.
/// </summary>
internal sealed class HoldQueue(ILibraryDbContext db, LoanPolicies policies, LibraryNotices notices)
{
    public async Task PassOnAsync(BookCopy copy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Tracked query, then filtered again in memory: reservations changed earlier in this unit of work (not saved
        // yet) must count with their new status.
        var queue = await db.Reservations
            .Where(r => r.BookId == copy.BookId && r.Status == ReservationStatus.Waiting)
            .ToListAsync(cancellationToken);
        var next = queue
            .Where(r => r.Status == ReservationStatus.Waiting)
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .FirstOrDefault();

        if (next is null)
        {
            if (copy.Status is CopyStatus.OnLoan or CopyStatus.OnHold)
                copy.ReturnToShelf();
            return;
        }

        var role = await policies.RoleOfAsync(next.UserId, cancellationToken) ?? LoanPolicy.Student;
        var policy = await policies.ForRoleAsync(role, cancellationToken);
        copy.HoldFor(next.Id);
        next.MarkReady(copy.Id, now, policy.HoldDays);

        var title = await db.Books.AsNoTracking().Where(b => b.Id == copy.BookId).Select(b => b.Title).SingleAsync(cancellationToken);
        notices.Ready(next, title, now);
    }
}
