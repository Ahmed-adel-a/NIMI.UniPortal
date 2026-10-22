namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>POST /api/forum/threads/{id}/posts and PUT /api/forum/posts/{id}.</summary>
public sealed record PostRequest(string? Body);
