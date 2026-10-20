namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

/// <summary>PUT /api/admin/departments/{id} body (the id comes from the route).</summary>
public sealed record UpdateDepartmentRequest(Guid FacultyId, string Code, string Name, string NameAr, string? Description, bool IsActive);
