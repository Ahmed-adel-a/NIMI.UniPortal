namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>A faculty's own setting (null when it inherits the university's) and the system its departments inherit.</summary>
public sealed record FacultyAcademicSystemDto(
    Guid FacultyId, string Code, string Name, string NameAr, string? Override, string Effective);
