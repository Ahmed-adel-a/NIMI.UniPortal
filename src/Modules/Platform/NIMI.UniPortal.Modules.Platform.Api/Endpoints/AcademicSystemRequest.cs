namespace NIMI.UniPortal.Modules.Platform.Api.Endpoints;

/// <summary><c>credit_hours</c>, <c>year_groups</c>, or null to inherit from the level above.</summary>
public sealed record AcademicSystemRequest(string? AcademicSystem);
