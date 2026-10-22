namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>POST / PUT /api/admin/forum/categories (<see cref="IsActive"/> is ignored on create).</summary>
public sealed record CategoryRequest(string? Name, string? NameAr, Guid? FacultyId, int SortOrder, bool? IsActive);
