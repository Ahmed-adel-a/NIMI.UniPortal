using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

/// <summary>
/// Decision LIFECYCLEDEPT: Student Affairs sets the academic system of the university (no faculty, no department), a
/// faculty or a department. A null system clears that level's setting, so it inherits again. Refused (409) when a
/// department whose system would change still has students with records in its current system.
/// </summary>
public sealed record SetAcademicSystemCommand(Guid? FacultyId, Guid? DepartmentId, string? AcademicSystem)
    : ICommand<AcademicSystemsOverviewDto>;
