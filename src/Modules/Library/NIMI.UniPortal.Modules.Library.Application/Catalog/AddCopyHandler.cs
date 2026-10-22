using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Catalog;

internal sealed class AddCopyHandler(ILibraryDbContext db, HoldQueue holds, TimeProvider clock) : IRequestHandler<AddCopyCommand, CopyDto>
{
    public async Task<CopyDto> Handle(AddCopyCommand command, CancellationToken cancellationToken)
    {
        if (!await db.Books.AsNoTracking().AnyAsync(b => b.Id == command.BookId, cancellationToken))
            throw new NotFoundException("Book not found.");

        var now = clock.GetUtcNow();
        var copy = BookCopy.Add(command.BookId, command.Barcode ?? string.Empty, command.Shelf, now);
        if (await db.Copies.AnyAsync(c => c.Barcode == copy.Barcode, cancellationToken))
            throw new ConflictException($"Barcode {copy.Barcode} is already in use.");

        db.Copies.Add(copy);

        // A new copy of a book people queue for goes straight to the first of them (otherwise it stays on the shelf).
        await holds.PassOnAsync(copy, now, cancellationToken);
        return CopyDto.From(copy);
    }
}
