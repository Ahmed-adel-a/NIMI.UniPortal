using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Terms;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.GetCurrentTerm;

internal sealed class GetCurrentTermHandler(ISchedulingDbContext db)
    : IRequestHandler<GetCurrentTermQuery, TermDto>
{
    public async Task<TermDto> Handle(GetCurrentTermQuery query, CancellationToken cancellationToken)
    {
        var term = await db.Terms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Status == TermStatus.Active, cancellationToken)
            ?? throw new NotFoundException("There is no active term.");

        return TermDto.From(term);
    }
}
