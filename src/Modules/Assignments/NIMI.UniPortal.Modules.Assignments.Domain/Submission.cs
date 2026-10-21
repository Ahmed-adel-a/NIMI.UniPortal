namespace NIMI.UniPortal.Modules.Assignments.Domain;

/// <summary>
/// One student's file for one assignment, handed in before the deadline (R3), with a receipt number as proof (R6).
/// It is reviewed once: approved with a grade, or rejected with a reason (R7). Name and code are snapshots.
/// </summary>
public sealed class Submission
{
    private const int MaxFeedbackLength = 4000;
    private const int MaxFileNameLength = 255;

    private Submission()
    {
    }

    public Guid Id { get; private set; }

    public Guid AssignmentId { get; private set; }

    public Guid StudentId { get; private set; }

    public string StudentName { get; private set; } = null!;

    public string? StudentCode { get; private set; }

    public string StorageKey { get; private set; } = null!;

    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long SizeBytes { get; private set; }

    public DateTimeOffset SubmittedAt { get; private set; }

    public string ReceiptNumber { get; private set; } = null!;

    public SubmissionStatus Status { get; private set; }

    public decimal? Grade { get; private set; }

    public string? Feedback { get; private set; }

    public Guid? ReviewedBy { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    public static Submission Create(
        Assignment assignment,
        Guid studentId,
        string studentName,
        string? studentCode,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        string receiptNumber,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        if (!assignment.IsOpenAt(now))
            throw new InvalidOperationException("The deadline has passed.");

        var cleanName = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (!assignment.Allows(cleanName))
            throw new ArgumentException($"Allowed file types: {assignment.AllowedExtensions}.", "file");
        if (sizeBytes < 1 || sizeBytes > assignment.MaxFileMb * 1024L * 1024L)
            throw new ArgumentException($"A file must be between 1 byte and {assignment.MaxFileMb} MB.", "file");
        if (cleanName.Length > MaxFileNameLength)
            throw new ArgumentException($"At most {MaxFileNameLength} characters in the file name.", "file");

        return new Submission
        {
            Id = Guid.CreateVersion7(),
            AssignmentId = assignment.Id,
            StudentId = studentId,
            StudentName = studentName ?? string.Empty,
            StudentCode = studentCode,
            StorageKey = string.IsNullOrWhiteSpace(storageKey) ? throw new ArgumentException("Required.", nameof(storageKey)) : storageKey,
            FileName = cleanName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            SizeBytes = sizeBytes,
            SubmittedAt = now,
            ReceiptNumber = string.IsNullOrWhiteSpace(receiptNumber) ? throw new ArgumentException("Required.", nameof(receiptNumber)) : receiptNumber,
            Status = SubmissionStatus.Submitted,
        };
    }

    public void Approve(decimal grade, decimal maxGrade, string? feedback, Guid reviewedBy, DateTimeOffset now)
    {
        EnsureNotReviewed();
        if (grade < 0m || grade > maxGrade)
            throw new ArgumentOutOfRangeException("grade", grade, $"The grade must be between 0 and {maxGrade}.");

        var trimmed = feedback?.Trim();
        if (trimmed?.Length > MaxFeedbackLength)
            throw new ArgumentException($"At most {MaxFeedbackLength} characters.", "feedback");

        Status = SubmissionStatus.Approved;
        Grade = grade;
        Feedback = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        ReviewedBy = reviewedBy;
        ReviewedAt = now;
    }

    public void Reject(string reason, Guid reviewedBy, DateTimeOffset now)
    {
        EnsureNotReviewed();
        var trimmed = reason?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new ArgumentException("Say why the submission is rejected.", "rejectionReason");
        if (trimmed.Length > MaxFeedbackLength)
            throw new ArgumentException($"At most {MaxFeedbackLength} characters.", "rejectionReason");

        Status = SubmissionStatus.Rejected;
        Feedback = trimmed;
        ReviewedBy = reviewedBy;
        ReviewedAt = now;
    }

    private void EnsureNotReviewed()
    {
        if (Status != SubmissionStatus.Submitted)
            throw new InvalidOperationException("This submission has already been reviewed.");
    }
}
