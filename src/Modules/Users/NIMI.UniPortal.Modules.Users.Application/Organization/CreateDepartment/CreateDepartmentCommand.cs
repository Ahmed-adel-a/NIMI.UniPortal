using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.CreateDepartment;

public sealed record CreateDepartmentCommand(Guid FacultyId, string Code, string Name, string NameAr, string? Description)
    : ICommand<DepartmentDto>;
