namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

/// <summary>
/// One student's year as the rule sees it: the current and final level, the years already spent in the current
/// level (1 = first year there), the subjects failed and not passed since (any level), and whether fees are
/// outstanding.
/// </summary>
public sealed record PromotionInput(int Level, int FinalLevel, int YearsInLevel, int FailedSubjects, bool FeesOutstanding);
