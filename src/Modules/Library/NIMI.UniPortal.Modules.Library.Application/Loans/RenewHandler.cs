using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.Modules.Library.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

internal sealed class RenewHandler(ILibraryDbContext db, LoanPolicies policies, LoanPresenter presenter, TimeProvider clock)
    : IRequestHandler<RenewCommand, LoanDto>
{
    public async Task<LoanDto> Handle(RenewCommand command, CancellationToken cancellationToken)
    {
        var loan = await db.Loans.SingleOrDefaultAsync(l => l.Id == command.LoanId && l.UserId == command.Caller.UserId, cancellationToken)
            ?? throw new NotFoundException("Loan not found.");
        if (await db.Reservations.AnyAsync(r => r.BookId == loan.BookId && r.Status == ReservationStatus.Waiting, cancellationToken))
            throw new ConflictException("Another member is waiting for this book; please return it.");

        var role = await policies.RoleOfAsync(command.Caller.UserId, cancellationToken)
            ?? throw new ForbiddenException("Only active students and professors borrow books.");
        var policy = await policies.ForRoleAsync(role, cancellationToken);
        LibraryErrors.AsConflict(() => loan.Renew(clock.GetUtcNow(), policy.LoanDays, policy.MaxRenewals));
        return await presenter.PresentAsync(loan, cancellationToken);
    }
}
