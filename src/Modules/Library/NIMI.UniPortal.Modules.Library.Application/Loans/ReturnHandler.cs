using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Library.Application.Abstractions;
using NIMI.UniPortal.Modules.Library.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Library.Application.Loans;

internal sealed class ReturnHandler(ILibraryDbContext db, HoldQueue holds, LoanPresenter presenter, TimeProvider clock)
    : IRequestHandler<ReturnCommand, LoanDto>
{
    public async Task<LoanDto> Handle(ReturnCommand command, CancellationToken cancellationToken)
    {
        var loan = await db.Loans.SingleOrDefaultAsync(l => l.Id == command.LoanId, cancellationToken)
            ?? throw new NotFoundException("Loan not found.");
        var now = clock.GetUtcNow();
        LibraryErrors.AsConflict(() => loan.Return(now));

        var copy = await db.Copies.SingleAsync(c => c.Id == loan.CopyId, cancellationToken);
        await holds.PassOnAsync(copy, now, cancellationToken);
        return await presenter.PresentAsync(loan, cancellationToken);
    }
}
