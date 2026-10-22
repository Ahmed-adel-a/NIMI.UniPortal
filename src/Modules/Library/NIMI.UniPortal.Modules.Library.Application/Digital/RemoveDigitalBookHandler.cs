using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Digital;

internal sealed class RemoveDigitalBookHandler(ILibraryDbContext db, TimeProvider clock) : IRequestHandler<RemoveDigitalBookCommand, Unit>
{
    public async Task<Unit> Handle(RemoveDigitalBookCommand command, CancellationToken cancellationToken)
    {
        var book = await db.DigitalBooks.SingleOrDefaultAsync(b => b.Id == command.Id && b.RemovedAt == null, cancellationToken)
            ?? throw new NotFoundException($"No digital book with id '{command.Id}'.");
        book.Remove(clock.GetUtcNow());
        return Unit.Value;
    }
}
