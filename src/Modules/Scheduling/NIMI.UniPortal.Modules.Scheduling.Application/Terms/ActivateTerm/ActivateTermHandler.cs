using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.ActivateTerm;

internal sealed class ActivateTermHandler(ISchedulingDbContext db, TimeProvider clock)
    : IRequestHandler<ActivateTermCommand, TermDto>
{
    public async Task<TermDto> Handle(ActivateTermCommand command, CancellationToken cancellationToken)
    {
        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No term with id '{command.Id}'.");

        if (term.Status != TermStatus.Planned)
            throw new ConflictException("Only a planned term can be activated.");

        var now = clock.GetUtcNow();
        var previouslyActive = await db.Terms
            .FirstOrDefaultAsync(t => t.Status == TermStatus.Active, cancellationToken);
        previouslyActive?.Close(now);

        term.Activate(now);
        return TermDto.From(term);
    }
}
