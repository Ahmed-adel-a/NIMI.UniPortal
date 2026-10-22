namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

public sealed record SavePromotionPolicyRequest(
    int MaxCarriedSubjects, IReadOnlyList<int>? GateLevels, int MaxYearsPerLevel, bool RequireFeesPaid);
