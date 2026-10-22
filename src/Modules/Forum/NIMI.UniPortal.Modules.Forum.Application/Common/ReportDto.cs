using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>A report; <c>status</c> and <c>resolution</c> serialize as snake_case text.</summary>
public sealed record ReportDto(
    Guid Id,
    Guid? ThreadId,
    Guid? PostId,
    Guid ReportedBy,
    string Reason,
    ForumReportStatus Status,
    ForumReportResolution? Resolution,
    Guid? ResolvedBy,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset CreatedAt)
{
    public static ReportDto From(ForumReport report) =>
        new(report.Id, report.ThreadId, report.PostId, report.ReportedBy, report.Reason, report.Status, report.Resolution,
            report.ResolvedBy, report.ResolvedAt, report.CreatedAt);
}
