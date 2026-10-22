namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>
/// The academic system in force (<c>credit_hours</c> or <c>year_groups</c>) and which level decided it
/// (<c>department</c>, <c>faculty</c>, <c>university</c> or <c>default</c>).
/// </summary>
public sealed record AcademicSystemDto(string AcademicSystem, string Source);
