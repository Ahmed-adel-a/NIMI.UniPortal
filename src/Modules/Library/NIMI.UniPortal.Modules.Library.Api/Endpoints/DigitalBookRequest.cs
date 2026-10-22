namespace NIMI.UniPortal.Modules.Library.Api.Endpoints;

/// <summary>PUT /api/admin/library/digital/{id} body.</summary>
public sealed record DigitalBookRequest(
    string? Title, string? Authors, string? Description, Guid? FacultyId, Guid? DepartmentId, Guid? SubjectId);
