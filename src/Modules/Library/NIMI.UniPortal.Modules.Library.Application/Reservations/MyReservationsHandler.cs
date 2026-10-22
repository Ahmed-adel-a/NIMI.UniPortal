using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Reservations;

internal sealed class MyReservationsHandler(ILibraryDbContext db) : IRequestHandler<MyReservationsQuery, PagedResult<ReservationDto>>
{
    public async Task<PagedResult<ReservationDto>> Handle(MyReservationsQuery query, CancellationToken cancellationToken)
    {
        var mine = db.Reservations.AsNoTracking().Where(r => r.UserId == query.Caller.UserId);
        var total = await mine.CountAsync(cancellationToken);
        var page = await mine
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var bookIds = page.Select(r => r.BookId).Distinct().ToList();
        var titles = await db.Books.AsNoTracking().Where(b => bookIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Title, cancellationToken);
        var waiting = await db.Reservations.AsNoTracking()
            .Where(r => bookIds.Contains(r.BookId) && r.Status == ReservationStatus.Waiting)
            .Select(r => new { r.BookId, r.CreatedAt, r.Id })
            .ToListAsync(cancellationToken);

        var items = page.Select(r => new ReservationDto(
            r.Id,
            r.BookId,
            titles.GetValueOrDefault(r.BookId) ?? string.Empty,
            r.Status,
            r.Status == ReservationStatus.Waiting
                ? 1 + waiting.Count(w => w.BookId == r.BookId && (w.CreatedAt < r.CreatedAt || (w.CreatedAt == r.CreatedAt && w.Id.CompareTo(r.Id) < 0)))
                : null,
            r.CreatedAt,
            r.ReadyAt,
            r.ExpiresAt)).ToList();

        return new PagedResult<ReservationDto>(items, query.Paging.Page, query.Paging.PageSize, total);
    }
}
