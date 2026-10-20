using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Users.Application.Organization.UpdateDepartment;

/// <summary>Full replace; may move the department to another (active) faculty. IsActive = false deactivates.</summary>
public sealed record UpdateDepartmentCommand(
    Guid Id,
    Guid FacultyId,
    string Code,
    string Name,
    string NameAr,
    string? Description,
    bool IsActive) : ICommand<DepartmentDto>;
