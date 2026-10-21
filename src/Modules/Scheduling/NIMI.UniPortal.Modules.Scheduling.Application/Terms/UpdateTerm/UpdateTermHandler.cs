using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.UpdateTerm;

internal sealed class UpdateTermHandler(ISchedulingDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateTermCommand, TermDto>
{
    public async Task<TermDto> Handle(UpdateTermCommand command, CancellationToken cancellationToken)
    {
        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No term with id '{command.Id}'.");

        if (term.Status != TermStatus.Planned)
            throw new ConflictException("Only a planned term can be edited.");

        term.Update(
            command.AcademicYear, command.Number, command.StartsOn, command.EndsOn, command.WeekCount, clock.GetUtcNow());

        return TermDto.From(term);
    }
}
