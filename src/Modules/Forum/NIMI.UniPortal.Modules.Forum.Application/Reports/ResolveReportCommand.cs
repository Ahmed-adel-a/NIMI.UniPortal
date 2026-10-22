using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Reports;

/// <summary>Student Affairs resolves a report once: <c>remove</c> the content or <c>dismiss</c> the report.</summary>
public sealed record ResolveReportCommand(Guid ReportId, string? Action, Viewer Caller) : ICommand<ReportDto>;
