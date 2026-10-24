using System.Runtime.CompilerServices;

namespace NIMI.UniPortal.Modules.StudentReports.Domain;

/// <summary>
/// Decision ROLES2 — a student's report to the head of their department. The department is copied when it is sent,
/// so a later head of that department sees it too. The head answers once and/or closes it; nothing is deleted.
/// </summary>
public sealed class StudentReport
{
    public const int MaxTitleLength = 200;
    public const int MaxTextLength = 4000;

    private StudentReport()
    {
    }

    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public ReportCategory Category { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public ReportStatus Status { get; private set; }

    public string? Reply { get; private set; }

    public Guid? AnsweredBy { get; private set; }

    public DateTimeOffset? AnsweredAt { get; private set; }

    public Guid? ClosedBy { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>One reply per report, and not once it is closed.</summary>
    public bool CanAnswer => Status == ReportStatus.Open;

    public bool CanClose => Status != ReportStatus.Closed;

    public static StudentReport Submit(
        Guid studentId, Guid departmentId, ReportCategory category, string title, string message, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            StudentId = Required(studentId, "studentId"),
            DepartmentId = Required(departmentId, "departmentId"),
            Category = Enum.IsDefined(category) ? category : throw new ArgumentOutOfRangeException("category", category, "Unknown category."),
            Title = Text(title, MaxTitleLength, "title"),
            Message = Text(message, MaxTextLength, "message"),
            Status = ReportStatus.Open,
            CreatedAt = now,
        };

    public void Answer(Guid headId, string reply, DateTimeOffset now)
    {
        if (!CanAnswer)
            throw new InvalidOperationException(Status == ReportStatus.Closed ? "The report is closed." : "The report was already answered.");
        Reply = Text(reply, MaxTextLength, "reply");
        AnsweredBy = Required(headId, "headId");
        AnsweredAt = now;
        Status = ReportStatus.Answered;
        UpdatedAt = now;
    }

    public void Close(Guid headId, DateTimeOffset now)
    {
        if (!CanClose)
            throw new InvalidOperationException("The report is already closed.");
        ClosedBy = Required(headId, "headId");
        ClosedAt = now;
        Status = ReportStatus.Closed;
        UpdatedAt = now;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Text(string value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Required.", field);
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : throw new ArgumentException($"At most {maxLength} characters.", field);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Guid Required(Guid id, string field) => id != Guid.Empty ? id : throw new ArgumentException("Required.", field);
}
