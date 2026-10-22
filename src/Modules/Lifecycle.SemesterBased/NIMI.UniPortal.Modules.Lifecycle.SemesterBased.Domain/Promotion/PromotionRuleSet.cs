namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

/// <summary>
/// The promotion numbers in force for a student's faculty: how many failed subjects may be carried up, the stage
/// gates (levels entered only with every lower subject passed), the years allowed in one level, and whether
/// outstanding fees hold the decision.
/// </summary>
public sealed record PromotionRuleSet(
    int MaxCarriedSubjects,
    IReadOnlyList<int> GateLevels,
    int MaxYearsPerLevel,
    bool RequireFeesPaid);
