using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Forum.Application.Reports;

/// <summary>A member flags exactly one visible thread or post for Student Affairs.</summary>
public sealed record FileReportCommand(Guid? ThreadId, Guid? PostId, string? Reason, Viewer Caller) : ICommand<ReportDto>;
