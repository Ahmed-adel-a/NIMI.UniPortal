namespace NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;

/// <summary>Which level decided a department's academic system.</summary>
public enum AcademicSystemSource
{
    Department,
    Faculty,
    University,

    /// <summary>Nothing is set: the deployment's built-in default applies.</summary>
    Default,
}
