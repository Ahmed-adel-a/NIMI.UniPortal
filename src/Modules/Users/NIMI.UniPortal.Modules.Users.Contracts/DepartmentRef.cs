namespace NIMI.UniPortal.Modules.Users.Contracts;

/// <summary>A department as other modules see it.</summary>
public sealed record DepartmentRef(Guid Id, Guid FacultyId, string Code, string Name, string NameAr, bool IsActive);
