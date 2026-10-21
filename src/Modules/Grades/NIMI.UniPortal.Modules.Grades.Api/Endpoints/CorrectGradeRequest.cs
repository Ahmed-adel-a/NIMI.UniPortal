namespace NIMI.UniPortal.Modules.Grades.Api.Endpoints;

/// <summary>POST /api/admin/grades/{id}/correct.</summary>
public sealed record CorrectGradeRequest(decimal Score, string? Reason);
