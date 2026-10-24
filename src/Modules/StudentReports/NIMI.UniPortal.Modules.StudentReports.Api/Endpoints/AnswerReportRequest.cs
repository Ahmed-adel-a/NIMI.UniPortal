namespace NIMI.UniPortal.Modules.StudentReports.Api.Endpoints;

/// <summary>POST /api/department/reports/{id}/answer body: a reply, close, or both.</summary>
public sealed record AnswerReportRequest(string? Reply, bool Close);
