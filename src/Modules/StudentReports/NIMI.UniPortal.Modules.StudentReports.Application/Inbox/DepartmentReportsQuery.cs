using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Inbox;

/// <summary>The head's inbox: their department's reports, newest first, optionally one status (open, answered, closed).</summary>
public sealed record DepartmentReportsQuery(Viewer Caller, string? Status, PageRequest Paging) : IQuery<PagedResult<ReportDto>>;
