namespace NIMI.UniPortal.Modules.Platform.Domain.Settings;

/// <summary>How students end up in military education (decision D5).</summary>
public enum MilitaryEducationMode
{
    /// <summary>The student applies for it.</summary>
    OptIn = 0,

    /// <summary>Assigned to students automatically by the configured timing/criteria.</summary>
    Mandatory = 1,
}
