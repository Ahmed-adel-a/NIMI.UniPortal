namespace NIMI.UniPortal.Modules.Users.Contracts;

/// <summary>
/// A student's academic placement (P5.0): where they belong and when they enrolled. Payments uses it to decide
/// which fee items apply and to build fee reports. Null when the id is not a student.
/// </summary>
public sealed record StudentPlacementRef(
    Guid StudentId,
    Guid DepartmentId,
    Guid FacultyId,
    int EnrollmentYear,
    bool IsActive);
