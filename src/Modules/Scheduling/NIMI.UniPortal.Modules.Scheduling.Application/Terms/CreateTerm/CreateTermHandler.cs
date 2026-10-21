using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CreateTerm;

internal sealed class CreateTermHandler(ISchedulingDbContext db, TimeProvider clock)
    : IRequestHandler<CreateTermCommand, TermDto>
{
    public async Task<TermDto> Handle(CreateTermCommand command, CancellationToken cancellationToken)
    {
        var term = Term.Create(
            command.AcademicYear, command.Number, command.StartsOn, command.EndsOn, command.WeekCount, clock.GetUtcNow());

        var duplicate = await db.Terms.AnyAsync(
            t => t.AcademicYear == term.AcademicYear && t.Number == term.Number, cancellationToken);
        if (duplicate)
            throw new ConflictException(
                $"A {term.Number.ToString().ToLowerInvariant()} term for {term.AcademicYear} already exists.");

        db.Terms.Add(term);
        return TermDto.From(term);
    }
}
