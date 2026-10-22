namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>A department's own setting (null when it inherits), the system in force and which level decided it.</summary>
public sealed record DepartmentAcademicSystemDto(
    Guid DepartmentId, Guid FacultyId, string Code, string Name, string NameAr, string? Override, string Effective, string Source);
