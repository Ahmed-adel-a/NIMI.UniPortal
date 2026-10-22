namespace NIMI.UniPortal.Modules.Events.Api.Endpoints;

/// <summary>POST /api/admin/events/{id}/check-in.</summary>
public sealed record CheckInRequest(Guid UserId);
