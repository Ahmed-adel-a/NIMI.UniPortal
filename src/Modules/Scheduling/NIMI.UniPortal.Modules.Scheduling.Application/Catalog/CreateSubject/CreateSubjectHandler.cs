using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateSubject;

internal sealed class CreateSubjectHandler(ISchedulingDbContext db, IUsersApi users, TimeProvider clock)
    : IRequestHandler<CreateSubjectCommand, SubjectDto>
{
    public async Task<SubjectDto> Handle(CreateSubjectCommand command, CancellationToken cancellationToken)
    {
        _ = await users.GetDepartmentAsync(command.DepartmentId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.DepartmentId), "Unknown department.");

        var subject = Subject.Create(
            command.DepartmentId,
            command.Code,
            command.Name,
            command.NameAr,
            command.Description,
            command.CreditHours,
            command.Category,
            command.IsGraduationRequirement,
            clock.GetUtcNow());

        if (await db.Subjects.AnyAsync(s => s.Code == subject.Code, cancellationToken))
            throw new ConflictException($"A subject with code '{subject.Code}' already exists.");

        db.Subjects.Add(subject);
        return SubjectDto.From(subject);
    }
}
