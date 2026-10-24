using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Oversight;

/// <summary>Student Affairs: every report, optionally one department and/or one status, newest first.</summary>
public sealed record AllReportsQuery(Guid? DepartmentId, string? Status, PageRequest Paging) : IQuery<PagedResult<StudentReportDto>>;
