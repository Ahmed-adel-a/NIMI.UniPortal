using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Answer;

/// <summary>
/// The department head replies (once) and/or closes a report of their department; at least one of the two. The
/// student is notified.
/// </summary>
public sealed record AnswerReportCommand(Viewer Caller, Guid ReportId, string? Reply, bool Close) : ICommand<StudentReportDto>;
