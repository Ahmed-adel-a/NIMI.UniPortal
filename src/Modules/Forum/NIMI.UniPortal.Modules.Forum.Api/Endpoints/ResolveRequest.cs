namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>POST /api/admin/forum/reports/{id}/resolve: <c>remove</c> or <c>dismiss</c>.</summary>
public sealed record ResolveRequest(string? Action);
