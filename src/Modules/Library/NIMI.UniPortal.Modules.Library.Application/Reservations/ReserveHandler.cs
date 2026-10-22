using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Reservations;

/// <summary>
/// Only members reserve; only when no copy is on the shelf; not a book they hold; one active reservation per book (409).
/// </summary>
internal sealed class ReserveHandler(ILibraryDbContext db, LoanPolicies policies, TimeProvider clock)
    : IRequestHandler<ReserveCommand, ReservationDto>
{
    public async Task<ReservationDto> Handle(ReserveCommand command, CancellationToken cancellationToken)
    {
        if (await policies.RoleOfAsync(command.Caller.UserId, cancellationToken) is null)
            throw new ForbiddenException("Only active students and professors reserve books.");

        var book = await db.Books.AsNoTracking().SingleOrDefaultAsync(b => b.Id == command.BookId, cancellationToken)
            ?? throw new NotFoundException("Book not found.");
        if (await db.Copies.AnyAsync(c => c.BookId == book.Id && c.Status == CopyStatus.Available, cancellationToken))
            throw new ConflictException("A copy is on the shelf — borrow it instead.");
        if (await db.Loans.AnyAsync(l => l.BookId == book.Id && l.UserId == command.Caller.UserId && l.ReturnedAt == null, cancellationToken))
            throw new ConflictException("You already have this book.");
        if (await db.Reservations.AnyAsync(
                r => r.BookId == book.Id && r.UserId == command.Caller.UserId &&
                     (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready),
                cancellationToken))
            throw new ConflictException("You already reserved this book.");

        var ahead = await db.Reservations.CountAsync(r => r.BookId == book.Id && r.Status == ReservationStatus.Waiting, cancellationToken);
        var reservation = Reservation.Place(book.Id, command.Caller.UserId, clock.GetUtcNow());
        db.Reservations.Add(reservation);
        return new ReservationDto(
            reservation.Id, book.Id, book.Title, reservation.Status, ahead + 1, reservation.CreatedAt, null, null);
    }
}
