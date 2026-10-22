namespace NIMI.UniPortal.Modules.Platform.Contracts;

/// <summary>
/// Settings other modules may read (decision SETTINGS). Always ask for the faculty: the faculty's own override
/// wins over the university-wide value (P1.4), so nothing about the university is hardcoded.
/// </summary>
public interface IPlatformSettingsApi
{
    /// <summary>The effective student-code format, e.g. <c>{DEPT}-{YEAR}-{SEQ:0000}</c>.</summary>
    Task<string> GetStudentCodeFormatAsync(Guid facultyId, CancellationToken cancellationToken = default);

    /// <summary>The rules that effectively apply to a faculty, or to the whole university when <paramref name="facultyId"/> is null.</summary>
    Task<EffectiveSettings> GetEffectiveSettingsAsync(Guid? facultyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decision LIFECYCLEDEPT: the academic system a department runs (<see cref="AcademicSystems"/>): its own rule, else its
    /// faculty's, else the university's, else the deployment default. Null asks for the university-wide value.
    /// </summary>
    Task<string> GetAcademicSystemAsync(Guid? departmentId, CancellationToken cancellationToken = default);
}
