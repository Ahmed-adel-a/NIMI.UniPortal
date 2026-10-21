using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.GetSubject;

internal sealed class GetSubjectHandler(ISchedulingDbContext db)
    : IRequestHandler<GetSubjectQuery, SubjectDto>
{
    public async Task<SubjectDto> Handle(GetSubjectQuery query, CancellationToken cancellationToken)
    {
        var subject = await db.Subjects.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.Id, cancellationToken)
            ?? throw new NotFoundException($"No subject with id '{query.Id}'.");

        return SubjectDto.From(subject);
    }
}
