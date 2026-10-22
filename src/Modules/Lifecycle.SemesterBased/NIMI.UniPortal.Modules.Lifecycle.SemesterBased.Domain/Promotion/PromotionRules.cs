namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

/// <summary>
/// Decision SEMPROMO, after the executive regulations of Law 49/1972 (art. 80–81) and each faculty's own numbers:
/// <list type="number">
/// <item>Outstanding fees hold the decision when the policy requires paid fees.</item>
/// <item>The final level graduates with everything passed; otherwise the student stays and re-sits what failed.</item>
/// <item>Everything passed moves up one level.</item>
/// <item>A stage gate (e.g. level 3 in a 2+2 programme) is entered only with every lower subject passed.</item>
/// <item>Up to the maximum number of failed subjects move up and carry them (مواد تخلف).</item>
/// <item>Otherwise the student repeats the level or, after the maximum years there, sits the failed subjects
/// "from outside" (من الخارج).</item>
/// </list>
/// Passed subjects are never re-sat (art. 81): the next year's enrollment takes only the failed ones.
/// </summary>
public static class PromotionRules
{
    public static PromotionOutcome Decide(PromotionInput input, PromotionRuleSet rules)
    {
        if (rules.RequireFeesPaid && input.FeesOutstanding)
            return new PromotionOutcome(PromotionResult.Held, input.Level, "Held until the outstanding fees are paid.");

        if (input.Level >= input.FinalLevel)
        {
            return input.FailedSubjects == 0
                ? new PromotionOutcome(PromotionResult.Graduated, input.Level, "Every subject of the programme is passed.")
                : Stay(input, rules, $"{input.FailedSubjects} subject(s) still to pass in the final level.");
        }

        var next = input.Level + 1;
        if (input.FailedSubjects == 0)
            return new PromotionOutcome(PromotionResult.Promoted, next, "Every subject is passed.");

        if (rules.GateLevels.Contains(next))
            return Stay(input, rules, $"Level {next} can only be entered with every earlier subject passed.");

        if (input.FailedSubjects <= rules.MaxCarriedSubjects)
        {
            return new PromotionOutcome(
                PromotionResult.PromotedWithCarried, next, $"{input.FailedSubjects} failed subject(s) carried to level {next}.");
        }

        return Stay(input, rules, $"{input.FailedSubjects} failed subjects; at most {rules.MaxCarriedSubjects} can be carried.");
    }

    private static PromotionOutcome Stay(PromotionInput input, PromotionRuleSet rules, string reason) =>
        input.YearsInLevel >= rules.MaxYearsPerLevel
            ? new PromotionOutcome(
                PromotionResult.External,
                input.Level,
                $"{reason} After {rules.MaxYearsPerLevel} years in level {input.Level} the failed subjects are sat from outside.")
            : new PromotionOutcome(PromotionResult.Repeat, input.Level, reason);
}
