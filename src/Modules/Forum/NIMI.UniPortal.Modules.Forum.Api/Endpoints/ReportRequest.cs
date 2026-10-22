namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>POST /api/forum/reports — exactly one of thread or post.</summary>
public sealed record ReportRequest(Guid? ThreadId, Guid? PostId, string? Reason);
