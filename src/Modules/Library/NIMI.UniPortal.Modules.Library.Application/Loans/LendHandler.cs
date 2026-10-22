using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

/// <summary>
/// Only active students and professors borrow (400 <c>userId</c>). The copy must be on the shelf, or held for this
/// member's reservation (fulfilled now); the member stays under their open-loan limit and has nothing overdue (409).
/// </summary>
internal sealed class LendHandler(ILibraryDbContext db, LoanPolicies policies, LoanPresenter presenter, TimeProvider clock)
    : IRequestHandler<LendCommand, LoanDto>
{
    public async Task<LoanDto> Handle(LendCommand command, CancellationToken cancellationToken)
    {
        var barcode = command.Barcode?.Trim();
        var copy = await db.Copies.SingleOrDefaultAsync(c => c.Barcode == barcode, cancellationToken)
            ?? throw new NotFoundException($"No copy with barcode '{barcode}'.");
        var role = await policies.RoleOfAsync(command.UserId, cancellationToken)
            ?? throw ValidationErrors.For("userId", "Only active students and professors borrow books.");

        var now = clock.GetUtcNow();
        Reservation? held = null;
        if (copy.Status == CopyStatus.OnHold)
        {
            held = await db.Reservations.SingleOrDefaultAsync(r => r.Id == copy.HeldForReservationId, cancellationToken);
            if (held is null || held.UserId != command.UserId)
                throw new ConflictException("This copy is held for another member's reservation.");
        }
        else if (copy.Status != CopyStatus.Available)
        {
            throw new ConflictException("This copy is not available.");
        }

        var policy = await policies.ForRoleAsync(role, cancellationToken);
        var open = await db.Loans.Where(l => l.UserId == command.UserId && l.ReturnedAt == null).ToListAsync(cancellationToken);
        if (open.Any(l => l.IsOverdue(now)))
            throw new ConflictException("This member has an overdue book; it must come back first.");
        if (open.Count >= policy.MaxOpenLoans)
            throw new ConflictException($"A {role} may hold at most {policy.MaxOpenLoans} books at a time.");

        copy.Lend();
        held?.Fulfill(now);
        var loan = Loan.Open(copy.Id, copy.BookId, command.UserId, now, policy.LoanDays);
        db.Loans.Add(loan);
        return await presenter.PresentAsync(loan, cancellationToken);
    }
}
