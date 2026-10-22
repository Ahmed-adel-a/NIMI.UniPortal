using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

/// <summary>An ISBN belongs to one catalog entry (409); the holding faculty must exist (400).</summary>
internal sealed class SaveBookHandler(ILibraryDbContext db, IUsersApi users, TimeProvider clock)
    : IRequestHandler<SaveBookCommand, BookDetailDto>
{
    public async Task<BookDetailDto> Handle(SaveBookCommand command, CancellationToken cancellationToken)
    {
        if (command.FacultyId is { } facultyId && await users.GetFacultyAsync(facultyId, cancellationToken) is null)
            throw ValidationErrors.For("facultyId", "Unknown faculty.");

        var now = clock.GetUtcNow();
        Book book;
        if (command.Id is { } id)
        {
            book = await db.Books.SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
                ?? throw new NotFoundException("Book not found.");
            book.Update(command.Isbn, command.Title ?? string.Empty, command.Authors ?? string.Empty, command.Publisher, command.PublicationYear, command.FacultyId, now);
        }
        else
        {
            book = Book.Create(command.Isbn, command.Title ?? string.Empty, command.Authors ?? string.Empty, command.Publisher, command.PublicationYear, command.FacultyId, now);
            db.Books.Add(book);
        }

        if (book.Isbn is { } isbn && await db.Books.AnyAsync(b => b.Isbn == isbn && b.Id != book.Id, cancellationToken))
            throw new ConflictException($"ISBN {isbn} is already in the catalog.");

        var copies = await db.Copies.AsNoTracking().Where(c => c.BookId == book.Id).OrderBy(c => c.Barcode).ToListAsync(cancellationToken);
        var waiting = await db.Reservations.AsNoTracking().CountAsync(r => r.BookId == book.Id && r.Status == ReservationStatus.Waiting, cancellationToken);
        return BookDetailDto.From(book, [.. copies.Select(CopyDto.From)], waiting);
    }
}
