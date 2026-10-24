// ACCEPTANCE TEST — written by the consultant for decision ROLES2 (student reports). Do not modify; report problems instead.
namespace NIMI.UniPortal.Modules.IntegrationTests.StudentReports;

/// <summary>ENG → CS (student Ahmed, its head) and EE (student Sara, its head), plus Student Affairs.</summary>
internal sealed record ReportCampus(
    HttpClient Admin,
    Guid Cs,
    HttpClient Ahmed,
    HttpClient Sara,
    HttpClient CsHead,
    HttpClient EeHead);
