namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>Decision LIFECYCLEDEPT: the whole picture for Student Affairs: the university value, every faculty and department.</summary>
public sealed record AcademicSystemsOverviewDto(
    AcademicSystemDto University,
    IReadOnlyList<FacultyAcademicSystemDto> Faculties,
    IReadOnlyList<DepartmentAcademicSystemDto> Departments);
