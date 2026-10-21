using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateGroup;

internal sealed class CreateGroupHandler(ISchedulingDbContext db, IUsersApi users, TimeProvider clock)
    : IRequestHandler<CreateGroupCommand, GroupDto>
{
    public async Task<GroupDto> Handle(CreateGroupCommand command, CancellationToken cancellationToken)
    {
        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == command.SubjectId, cancellationToken)
            ?? throw new NotFoundException($"No subject with id '{command.SubjectId}'.");

        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == command.TermId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.TermId), "Unknown term.");

        if (command.TargetDepartmentId is { } departmentId)
            _ = await users.GetDepartmentAsync(departmentId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.TargetDepartmentId), "Unknown department.");

        var group = SubjectGroup.Create(
            subject, term, command.GroupNumber, command.TargetDepartmentId, command.Capacity, clock.GetUtcNow());

        var duplicate = await db.SubjectGroups.AnyAsync(
            g => g.SubjectId == command.SubjectId && g.TermId == command.TermId && g.GroupNumber == command.GroupNumber,
            cancellationToken);
        if (duplicate)
            throw new ConflictException($"Group {command.GroupNumber} already exists for this subject in this term.");

        db.SubjectGroups.Add(group);
        return GroupDto.From(group, subject.Code);
    }
}
