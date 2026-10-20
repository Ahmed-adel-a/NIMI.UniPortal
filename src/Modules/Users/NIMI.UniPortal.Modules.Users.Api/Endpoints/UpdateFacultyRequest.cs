namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

/// <summary>PUT /api/admin/faculties/{id} body (the id comes from the route).</summary>
public sealed record UpdateFacultyRequest(string Code, string Name, string NameAr, bool IsActive);
