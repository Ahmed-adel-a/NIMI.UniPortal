namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>POST /api/forum/threads/{id}/lock.</summary>
public sealed record LockRequest(bool Locked);
