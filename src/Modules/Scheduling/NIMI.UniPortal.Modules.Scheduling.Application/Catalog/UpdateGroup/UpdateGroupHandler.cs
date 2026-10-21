using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.UpdateGroup;

internal sealed class UpdateGroupHandler(ISchedulingDbContext db, IUsersApi users, TimeProvider clock)
    : IRequestHandler<UpdateGroupCommand, GroupDto>
{
    public async Task<GroupDto> Handle(UpdateGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await db.SubjectGroups
            .Include(g => g.Subject)
            .FirstOrDefaultAsync(g => g.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No group with id '{command.Id}'.");

        if (command.TargetDepartmentId is { } departmentId)
            _ = await users.GetDepartmentAsync(departmentId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.TargetDepartmentId), "Unknown department.");

        group.Update(command.Capacity, command.TargetDepartmentId, command.IsActive, clock.GetUtcNow());
        return GroupDto.From(group, group.Subject.Code);
    }
}
