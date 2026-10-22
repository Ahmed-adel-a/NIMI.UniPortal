namespace NIMI.UniPortal.Modules.Library.Api.Endpoints;

/// <summary>POST /api/admin/library/books/{id}/copies.</summary>
public sealed record CopyRequest(string? Barcode, string? Shelf);
