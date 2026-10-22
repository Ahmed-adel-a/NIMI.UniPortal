using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

internal sealed class SearchBooksHandler(ILibraryDbContext db) : IRequestHandler<SearchBooksQuery, PagedResult<BookDto>>
{
    public async Task<PagedResult<BookDto>> Handle(SearchBooksQuery query, CancellationToken cancellationToken)
    {
        var books = db.Books.AsNoTracking();
        if (query.FacultyId is { } facultyId)
            books = books.Where(b => b.FacultyId == facultyId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            var digits = new string([.. query.Search.Where(char.IsAsciiDigit)]);
            books = books.Where(b =>
                b.Title.ToLower().Contains(term) ||
                b.Authors.ToLower().Contains(term) ||
                (digits.Length >= 10 && b.Isbn != null && b.Isbn.Contains(digits)));
        }

        var total = await books.CountAsync(cancellationToken);
        var page = await books
            .OrderBy(b => b.Title)
            .ThenBy(b => b.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var ids = page.Select(b => b.Id).ToList();
        var counts = await db.Copies.AsNoTracking()
            .Where(c => ids.Contains(c.BookId) && c.Status != CopyStatus.Lost && c.Status != CopyStatus.Withdrawn)
            .GroupBy(c => c.BookId)
            .Select(g => new { BookId = g.Key, Total = g.Count(), Available = g.Count(c => c.Status == CopyStatus.Available) })
            .ToDictionaryAsync(x => x.BookId, cancellationToken);

        return new PagedResult<BookDto>(
            [.. page.Select(b => new BookDto(
                b.Id, b.Isbn, b.Title, b.Authors, b.Publisher, b.PublicationYear, b.FacultyId,
                counts.TryGetValue(b.Id, out var c) ? c.Total : 0,
                counts.TryGetValue(b.Id, out var a) ? a.Available : 0))],
            query.Paging.Page,
            query.Paging.PageSize,
            total);
    }
}
