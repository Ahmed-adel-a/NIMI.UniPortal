using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Mine;

/// <summary>The student's own reports, newest first.</summary>
public sealed record MyReportsQuery(Viewer Caller, PageRequest Paging) : IQuery<PagedResult<StudentReportDto>>;
