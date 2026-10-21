using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CloseTerm;

internal sealed class CloseTermHandler(ISchedulingDbContext db, TimeProvider clock)
    : IRequestHandler<CloseTermCommand, TermDto>
{
    public async Task<TermDto> Handle(CloseTermCommand command, CancellationToken cancellationToken)
    {
        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No term with id '{command.Id}'.");

        if (term.Status != TermStatus.Active)
            throw new ConflictException("Only an active term can be closed.");

        term.Close(clock.GetUtcNow());
        return TermDto.From(term);
    }
}
