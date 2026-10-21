using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Graduation;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;

internal sealed class SetGraduationRequirementHandler(ICreditHoursDbContext db, IUsersApi users, TimeProvider clock)
    : IRequestHandler<SetGraduationRequirementCommand, GraduationRequirementDto>
{
    public async Task<GraduationRequirementDto> Handle(SetGraduationRequirementCommand command, CancellationToken cancellationToken)
    {
        _ = await users.GetDepartmentAsync(command.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("Department not found.");

        var now = clock.GetUtcNow();
        var requirement = await db.GraduationRequirements
            .SingleOrDefaultAsync(r => r.DepartmentId == command.DepartmentId, cancellationToken);
        if (requirement is null)
        {
            requirement = GraduationRequirement.Create(command.DepartmentId, command.RequiredCredits, command.AdminId, now);
            db.GraduationRequirements.Add(requirement);
        }
        else
        {
            requirement.Set(command.RequiredCredits, command.AdminId, now);
        }

        return new GraduationRequirementDto(requirement.DepartmentId, requirement.RequiredCredits, requirement.UpdatedAt);
    }
}
