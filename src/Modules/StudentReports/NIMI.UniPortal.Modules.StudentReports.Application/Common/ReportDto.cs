using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Common;

/// <summary>A report as the student, their department head and Student Affairs see it.</summary>
public sealed record ReportDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string? StudentCode,
    Guid DepartmentId,
    ReportCategory Category,
    string Title,
    string Message,
    ReportStatus Status,
    string? Reply,
    Guid? AnsweredBy,
    string? AnsweredByName,
    DateTimeOffset? AnsweredAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset CreatedAt);
