namespace NIMI.UniPortal.Modules.Library.Api.Endpoints;

/// <summary>PUT /api/admin/library/copies/{id}: <c>available</c>, <c>lost</c> or <c>withdrawn</c>, and the shelf.</summary>
public sealed record CopyUpdateRequest(string? Status, string? Shelf);
