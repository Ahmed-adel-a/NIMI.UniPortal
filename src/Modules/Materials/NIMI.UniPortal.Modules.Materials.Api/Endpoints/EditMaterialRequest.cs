namespace NIMI.UniPortal.Modules.Materials.Api.Endpoints;

/// <summary>PUT /api/materials/{id}: metadata only.</summary>
public sealed record EditMaterialRequest(string? Title, string? Description, int? WeekNumber);
