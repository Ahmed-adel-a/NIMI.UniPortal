using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Reservations;

internal sealed class CancelReservationHandler(ILibraryDbContext db, HoldQueue holds, TimeProvider clock)
    : IRequestHandler<CancelReservationCommand, Unit>
{
    public async Task<Unit> Handle(CancelReservationCommand command, CancellationToken cancellationToken)
    {
        var reservation = await db.Reservations
            .SingleOrDefaultAsync(r => r.Id == command.ReservationId && r.UserId == command.Caller.UserId, cancellationToken)
            ?? throw new NotFoundException("Reservation not found.");

        var now = clock.GetUtcNow();
        var wasReady = reservation.Status == ReservationStatus.Ready;
        LibraryErrors.AsConflict(() => reservation.Cancel(now));

        if (wasReady && reservation.CopyId is { } copyId)
        {
            var copy = await db.Copies.SingleAsync(c => c.Id == copyId, cancellationToken);
            await holds.PassOnAsync(copy, now, cancellationToken);
        }

        return Unit.Value;
    }
}
