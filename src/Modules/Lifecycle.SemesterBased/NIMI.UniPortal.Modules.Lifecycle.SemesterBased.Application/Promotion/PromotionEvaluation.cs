using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Promotion;

/// <summary>What the rule says for a student, with the number of subjects they still owe.</summary>
internal sealed record PromotionEvaluation(PromotionOutcome Outcome, int Failed);
