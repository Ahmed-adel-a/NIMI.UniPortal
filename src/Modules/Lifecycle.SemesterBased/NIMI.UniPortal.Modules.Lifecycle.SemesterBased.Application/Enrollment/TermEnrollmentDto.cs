using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;

/// <summary>A student taking one subject in one group this term.</summary>
public sealed record TermEnrollmentDto(
    Guid Id,
    Guid StudentId,
    Guid TermId,
    Guid SubjectId,
    Guid SubjectGroupId,
    int Level,
    bool IsCarried,
    EnrollmentStatus Status,
    decimal? GradePoints)
{
    public static TermEnrollmentDto From(TermEnrollment e) =>
        new(e.Id, e.StudentId, e.TermId, e.SubjectId, e.SubjectGroupId, e.Level, e.IsCarried, e.Status, e.GradePoints);
}
