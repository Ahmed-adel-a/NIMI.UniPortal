namespace NIMI.UniPortal.Modules.Platform.Contracts;

/// <summary>
/// Settings other modules may read (decision SETTINGS). Always ask for the faculty: today the university-wide
/// value comes back; once faculty overrides exist (P1.4) the faculty's own value wins.
/// </summary>
public interface IPlatformSettingsApi
{
    /// <summary>The student-code format, e.g. <c>{DEPT}-{YEAR}-{SEQ:0000}</c>.</summary>
    Task<string> GetStudentCodeFormatAsync(Guid facultyId, CancellationToken cancellationToken = default);
}
