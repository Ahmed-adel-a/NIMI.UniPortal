using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Domain;

namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>Adds titles and barcodes to loans (one query each, for a whole page).</summary>
internal sealed class LoanPresenter(ILibraryDbContext db, TimeProvider clock)
{
    public async Task<LoanDto> PresentAsync(Loan loan, CancellationToken cancellationToken) =>
        (await PresentAllAsync([loan], cancellationToken))[0];

    public async Task<IReadOnlyList<LoanDto>> PresentAllAsync(IReadOnlyList<Loan> loans, CancellationToken cancellationToken)
    {
        var bookIds = loans.Select(l => l.BookId).Distinct().ToList();
        var copyIds = loans.Select(l => l.CopyId).Distinct().ToList();
        var titles = await db.Books.AsNoTracking().Where(b => bookIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.Title, cancellationToken);
        var barcodes = await db.Copies.AsNoTracking().Where(c => copyIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Barcode, cancellationToken);
        var now = clock.GetUtcNow();

        return [.. loans.Select(l => new LoanDto(
            l.Id, l.BookId, titles.GetValueOrDefault(l.BookId) ?? string.Empty, l.CopyId, barcodes.GetValueOrDefault(l.CopyId) ?? string.Empty,
            l.UserId, l.BorrowedAt, l.DueAt, l.ReturnedAt, l.Renewals, l.IsOverdue(now)))];
    }
}
