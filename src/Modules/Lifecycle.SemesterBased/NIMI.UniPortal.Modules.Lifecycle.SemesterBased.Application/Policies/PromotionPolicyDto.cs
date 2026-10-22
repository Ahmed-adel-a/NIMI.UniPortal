namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Policies;

/// <summary>
/// The promotion numbers in force (decision SEMPROMO). <see cref="Source"/> says where they come from:
/// <c>faculty</c> (the faculty's own override), <c>university</c> (Student Affairs' university-wide row) or
/// <c>default</c> (built in: 3 carried subjects, no gates, 2 years per level, fees required).
/// </summary>
public sealed record PromotionPolicyDto(
    Guid? FacultyId,
    string Source,
    int MaxCarriedSubjects,
    IReadOnlyList<int> GateLevels,
    int MaxYearsPerLevel,
    bool RequireFeesPaid);
