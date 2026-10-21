using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.Assignments.Application.Mine;

/// <summary>One of the student's own submissions with its receipt and, once reviewed, the grade and feedback.</summary>
public sealed record MySubmissionDto(
    Guid Id,
    Guid AssignmentId,
    string AssignmentTitle,
    string SubjectCode,
    string ReceiptNumber,
    DateTimeOffset SubmittedAt,
    SubmissionStatus Status,
    decimal? Grade,
    decimal MaxGrade,
    string? Feedback);
