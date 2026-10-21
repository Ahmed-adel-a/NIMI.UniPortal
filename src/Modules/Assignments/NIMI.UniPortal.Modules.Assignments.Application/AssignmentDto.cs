using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.Assignments.Application;

/// <summary>
/// An assignment as the API shows it (phase §5a). <see cref="SubmissionCount"/> is for teaching staff and Student
/// Affairs; <see cref="MySubmissionStatus"/> is the calling student's own status (null when they haven't submitted).
/// </summary>
public sealed record AssignmentDto(
    Guid Id,
    Guid SubjectId,
    string SubjectCode,
    Guid? SubjectGroupId,
    string Title,
    string? Instructions,
    DateTimeOffset DueAt,
    decimal MaxGrade,
    IReadOnlyList<string> AllowedExtensions,
    int MaxFileMb,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    bool IsOpen,
    long DueInSeconds,
    int? SubmissionCount,
    SubmissionStatus? MySubmissionStatus);
