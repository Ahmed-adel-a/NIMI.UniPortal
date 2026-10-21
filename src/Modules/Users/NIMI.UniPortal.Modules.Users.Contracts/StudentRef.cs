namespace NIMI.UniPortal.Modules.Users.Contracts;

/// <summary>
/// An active student as listings in other modules see them (P9.0, graduation eligibility): who they are and where
/// they belong.
/// </summary>
public sealed record StudentRef(
    Guid StudentId,
    string FullName,
    string StudentCode,
    Guid DepartmentId,
    Guid FacultyId,
    int EnrollmentYear);
