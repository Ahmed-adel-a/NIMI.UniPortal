namespace NIMI.UniPortal.Modules.StudentReports.Api.Endpoints;

/// <summary>POST /api/reports body. Category: academic, attendance, conduct or other.</summary>
public sealed record SubmitReportRequest(string Category, string Title, string Message);
