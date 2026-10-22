using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

internal sealed class ListDigitalBooksHandler(ILibraryDbContext db, DigitalScope scope)
    : IRequestHandler<ListDigitalBooksQuery, PagedResult<DigitalBookDto>>
{
    public async Task<PagedResult<DigitalBookDto>> Handle(ListDigitalBooksQuery query, CancellationToken cancellationToken)
    {
        var books = db.DigitalBooks.AsNoTracking().Where(b => b.RemovedAt == null);
        if (!query.Caller.IsAdmin)
        {
            // The same audience rule as DigitalBook.IsFor, written out for SQL.
            var (facultyId, departmentId) = await scope.ReaderAsync(query.Caller.UserId, cancellationToken);
            books = books.Where(b =>
                (b.FacultyId == null && b.DepartmentId == null) ||
                (b.DepartmentId == null && b.FacultyId == facultyId) ||
                (b.DepartmentId != null && b.DepartmentId == departmentId));
        }

        if (query.SubjectId is { } subjectId)
            books = books.Where(b => b.SubjectId == subjectId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            books = books.Where(b => b.Title.ToLower().Contains(term) || b.Authors.ToLower().Contains(term));
        }

        var total = await books.CountAsync(cancellationToken);
        var page = await books
            .OrderByDescending(b => b.CreatedAt).ThenBy(b => b.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DigitalBookDto>([.. page.Select(DigitalBookDto.From)], query.Paging.Page, query.Paging.PageSize, total);
    }
}
