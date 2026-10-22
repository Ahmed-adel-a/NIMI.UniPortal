using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>The student's latest year-end decision.</summary>
public sealed record LastDecisionDto(
    int AcademicYear, PromotionResult Result, int FromLevel, int ToLevel, string Reason, DateTimeOffset DecidedAt);
