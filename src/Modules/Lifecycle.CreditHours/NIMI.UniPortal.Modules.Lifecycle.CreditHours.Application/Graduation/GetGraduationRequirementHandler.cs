using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;

internal sealed class GetGraduationRequirementHandler(ICreditHoursDbContext db, IUsersApi users)
    : IRequestHandler<GetGraduationRequirementQuery, GraduationRequirementDto>
{
    public async Task<GraduationRequirementDto> Handle(GetGraduationRequirementQuery query, CancellationToken cancellationToken)
    {
        _ = await users.GetDepartmentAsync(query.DepartmentId, cancellationToken)
            ?? throw new NotFoundException("Department not found.");

        var requirement = await db.GraduationRequirements.AsNoTracking()
            .SingleOrDefaultAsync(r => r.DepartmentId == query.DepartmentId, cancellationToken);
        return new GraduationRequirementDto(query.DepartmentId, requirement?.RequiredCredits, requirement?.UpdatedAt);
    }
}
