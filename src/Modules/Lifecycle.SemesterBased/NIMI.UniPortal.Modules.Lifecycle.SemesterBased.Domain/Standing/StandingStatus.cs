namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;

/// <summary>Where a student stands in their year (فرقة).</summary>
public enum StandingStatus
{
    /// <summary>Taking the level's plan subjects (plus any carried ones).</summary>
    Active,

    /// <summary>Repeating the level: only the failed subjects are taken again (art. 81).</summary>
    Repeating,

    /// <summary>Past the maximum years in the level: sits the failed subjects "from outside" (art. 80).</summary>
    External,

    /// <summary>The year-end decision waits for the outstanding fees.</summary>
    OnHold,

    Graduated,

    Withdrawn,
}
