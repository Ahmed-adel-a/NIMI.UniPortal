namespace NIMI.UniPortal.Modules.Library.Api.Endpoints;

/// <summary>POST /api/admin/library/loans.</summary>
public sealed record LendRequest(string? Barcode, Guid UserId);
