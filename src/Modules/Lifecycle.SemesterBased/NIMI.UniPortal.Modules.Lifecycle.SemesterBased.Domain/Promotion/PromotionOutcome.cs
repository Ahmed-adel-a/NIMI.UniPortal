namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

/// <summary>The decision and the level the student is in next year (unchanged unless promoted).</summary>
public sealed record PromotionOutcome(PromotionResult Result, int ToLevel, string Reason);
