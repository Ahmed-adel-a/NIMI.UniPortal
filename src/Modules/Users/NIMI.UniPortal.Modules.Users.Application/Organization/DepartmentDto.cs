namespace NIMI.UniPortal.Modules.Users.Application.Organization;

public sealed record DepartmentDto(
    Guid Id,
    Guid FacultyId,
    string FacultyCode,
    string Code,
    string Name,
    string NameAr,
    string? Description,
    bool IsActive);
