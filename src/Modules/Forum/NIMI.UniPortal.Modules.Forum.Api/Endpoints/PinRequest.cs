namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>POST /api/forum/threads/{id}/pin.</summary>
public sealed record PinRequest(bool Pinned);
