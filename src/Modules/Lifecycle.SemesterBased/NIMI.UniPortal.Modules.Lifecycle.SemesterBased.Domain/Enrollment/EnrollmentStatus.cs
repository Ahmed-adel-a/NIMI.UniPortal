namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;

/// <summary>A term enrollment's state: taught this term, then passed or failed when grades are published.</summary>
public enum EnrollmentStatus
{
    Enrolled,
    Passed,
    Failed,
    Withdrawn,
}
