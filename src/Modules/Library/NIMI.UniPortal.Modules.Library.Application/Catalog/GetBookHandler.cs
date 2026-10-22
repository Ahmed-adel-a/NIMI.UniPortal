using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

internal sealed class GetBookHandler(ILibraryDbContext db) : IRequestHandler<GetBookQuery, BookDetailDto>
{
    public async Task<BookDetailDto> Handle(GetBookQuery query, CancellationToken cancellationToken)
    {
        var book = await db.Books.AsNoTracking().SingleOrDefaultAsync(b => b.Id == query.BookId, cancellationToken)
            ?? throw new NotFoundException("Book not found.");
        var copies = await db.Copies.AsNoTracking()
            .Where(c => c.BookId == book.Id)
            .OrderBy(c => c.Barcode)
            .ToListAsync(cancellationToken);
        var waiting = await db.Reservations.AsNoTracking()
            .CountAsync(r => r.BookId == book.Id && r.Status == ReservationStatus.Waiting, cancellationToken);

        return BookDetailDto.From(book, [.. copies.Select(CopyDto.From)], waiting);
    }
}
