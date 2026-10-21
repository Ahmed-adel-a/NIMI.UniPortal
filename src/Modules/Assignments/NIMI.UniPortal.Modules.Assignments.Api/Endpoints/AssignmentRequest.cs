namespace NIMI.UniPortal.Modules.Assignments.Api.Endpoints;

/// <summary>POST / PUT /api/assignments (subject and group are ignored on PUT). Defaults: pdf,docx,zip and 10 MB.</summary>
public sealed record AssignmentRequest(
    Guid SubjectId,
    Guid? SubjectGroupId,
    string? Title,
    string? Instructions,
    DateTimeOffset DueAt,
    decimal MaxGrade,
    string? AllowedExtensions,
    int? MaxFileMb)
{
    public const string DefaultExtensions = "pdf,docx,zip";
    public const int DefaultMaxFileMb = 10;
}
