using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

/// <summary>One year-end decision; <see cref="DecidedBy"/> is null when the rule decided.</summary>
public sealed record PromotionDto(
    Guid Id,
    Guid StudentId,
    Guid DepartmentId,
    int AcademicYear,
    int FromLevel,
    int ToLevel,
    PromotionResult Result,
    int FailedSubjects,
    string Reason,
    DateTimeOffset DecidedAt,
    Guid? DecidedBy)
{
    public static PromotionDto From(YearPromotion p) => new(
        p.Id, p.StudentId, p.DepartmentId, p.AcademicYear, p.FromLevel, p.ToLevel, p.Result, p.FailedSubjects, p.Reason,
        p.DecidedAt, p.DecidedBy);
}
