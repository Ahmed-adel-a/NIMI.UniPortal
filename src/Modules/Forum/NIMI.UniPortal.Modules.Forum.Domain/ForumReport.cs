namespace NIMI.UniPortal.Modules.Forum.Domain;

/// <summary>A member flags one thread or one post for Student Affairs, with a reason.</summary>
public sealed class ForumReport
{
    public const int MaxReasonLength = 1000;

    private ForumReport()
    {
    }

    public Guid Id { get; private set; }

    public Guid? ThreadId { get; private set; }

    public Guid? PostId { get; private set; }

    public Guid ReportedBy { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public ForumReportStatus Status { get; private set; }

    public ForumReportResolution? Resolution { get; private set; }

    public Guid? ResolvedBy { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ForumReport File(Guid? threadId, Guid? postId, Guid reportedBy, string reason, DateTimeOffset now)
    {
        if ((threadId is null) == (postId is null))
            throw new ArgumentException("Report exactly one thread or one post.", nameof(threadId));

        return new ForumReport
        {
            Id = Guid.CreateVersion7(),
            ThreadId = threadId,
            PostId = postId,
            ReportedBy = reportedBy,
            Reason = ForumText.Required(reason, MaxReasonLength, nameof(reason)),
            Status = ForumReportStatus.Open,
            CreatedAt = now,
        };
    }

    public void Resolve(bool removeContent, Guid resolvedBy, DateTimeOffset now)
    {
        if (Status != ForumReportStatus.Open)
            throw new InvalidOperationException("This report has already been resolved.");

        Status = ForumReportStatus.Resolved;
        Resolution = removeContent ? ForumReportResolution.Removed : ForumReportResolution.Dismissed;
        ResolvedBy = resolvedBy;
        ResolvedAt = now;
    }
}
