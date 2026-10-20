namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

/// <summary>PATCH /api/admin/users/{id}/status body.</summary>
public sealed record SetUserStatusRequest(bool IsActive);
