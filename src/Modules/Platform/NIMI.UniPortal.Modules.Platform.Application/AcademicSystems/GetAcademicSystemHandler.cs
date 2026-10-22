using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

internal sealed class GetAcademicSystemHandler(AcademicSystemResolver resolver, IUsersApi users)
    : IRequestHandler<GetAcademicSystemQuery, AcademicSystemDto>
{
    public async Task<AcademicSystemDto> Handle(GetAcademicSystemQuery query, CancellationToken cancellationToken)
    {
        Guid? facultyId = null;
        if (query.DepartmentId is { } departmentId)
        {
            var department = await users.GetDepartmentAsync(departmentId, cancellationToken)
                ?? throw new NotFoundException($"No department with id '{departmentId}'.");
            facultyId = department.FacultyId;
        }

        var (system, source) = await resolver.ResolveAsync(facultyId, query.DepartmentId, cancellationToken);
        return new AcademicSystemDto(AcademicSystemNames.Name(system), AcademicSystemNames.Name(source));
    }
}
