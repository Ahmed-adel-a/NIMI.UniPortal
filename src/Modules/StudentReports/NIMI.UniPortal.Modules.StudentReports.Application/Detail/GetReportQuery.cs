using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.StudentReports.Application.Detail;

/// <summary>One report, for its student, the head of its department or Student Affairs; anyone else gets a 404.</summary>
public sealed record GetReportQuery(Viewer Caller, Guid ReportId) : IQuery<StudentReportDto>;
