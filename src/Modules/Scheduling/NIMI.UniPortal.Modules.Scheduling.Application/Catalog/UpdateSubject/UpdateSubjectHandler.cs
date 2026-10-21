using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.UpdateSubject;

internal sealed class UpdateSubjectHandler(ISchedulingDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateSubjectCommand, SubjectDto>
{
    public async Task<SubjectDto> Handle(UpdateSubjectCommand command, CancellationToken cancellationToken)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No subject with id '{command.Id}'.");

        subject.Update(
            command.Name,
            command.NameAr,
            command.Description,
            command.CreditHours,
            command.Category,
            command.IsGraduationRequirement,
            command.IsActive,
            clock.GetUtcNow());

        return SubjectDto.From(subject);
    }
}
