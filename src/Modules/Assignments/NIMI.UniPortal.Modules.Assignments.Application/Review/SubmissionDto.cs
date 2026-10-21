using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.Assignments.Application.Review;

/// <summary>A submission as teaching staff see it (phase §5a).</summary>
public sealed record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    Guid StudentId,
    string StudentName,
    string? StudentCode,
    string FileName,
    long SizeBytes,
    DateTimeOffset SubmittedAt,
    string ReceiptNumber,
    SubmissionStatus Status,
    decimal? Grade,
    string? Feedback,
    DateTimeOffset? ReviewedAt)
{
    public static SubmissionDto From(Submission s) =>
        new(s.Id, s.AssignmentId, s.StudentId, s.StudentName, s.StudentCode, s.FileName, s.SizeBytes, s.SubmittedAt,
            s.ReceiptNumber, s.Status, s.Grade, s.Feedback, s.ReviewedAt);
}
