namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>POST /api/forum/categories/{id}/threads and PUT /api/forum/threads/{id} (subject is ignored on PUT).</summary>
public sealed record ThreadRequest(string? Title, string? Body, Guid? SubjectId);
