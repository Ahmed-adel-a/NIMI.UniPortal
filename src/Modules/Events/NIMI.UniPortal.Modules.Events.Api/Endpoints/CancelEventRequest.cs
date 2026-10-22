namespace NIMI.UniPortal.Modules.Events.Api.Endpoints;

/// <summary>POST /api/admin/events/{id}/cancel.</summary>
public sealed record CancelEventRequest(string? Reason);
