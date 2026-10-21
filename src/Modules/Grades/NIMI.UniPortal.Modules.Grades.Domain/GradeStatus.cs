namespace NIMI.UniPortal.Modules.Grades.Domain;

/// <summary>Decision D3 — the professor drafts, Student Affairs confirms and publishes. Students see published grades only.</summary>
public enum GradeStatus
{
    Draft,
    Confirmed,
    Published,
}
