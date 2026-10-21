namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Results;

/// <summary>
/// P09 §6, the lifecycle's own copy (modules never share code): GPA = Σ(points × credits) / Σ(credits), 3 decimals
/// half away from zero; zero credits carry no weight; cumulative = the latest attempt of each subject.
/// </summary>
public static class GpaMath
{
    public static decimal? Of(IEnumerable<ResultLine> lines)
    {
        var credits = 0;
        var weighted = 0m;
        foreach (var line in lines)
        {
            credits += line.CreditHours;
            weighted += line.GradePoints * line.CreditHours;
        }

        return credits == 0 ? null : Math.Round(weighted / credits, 3, MidpointRounding.AwayFromZero);
    }

    /// <summary>The latest attempt per subject among lines from terms that started on or before <paramref name="asOf"/>.</summary>
    public static IReadOnlyList<ResultLine> LatestAttempts(IEnumerable<ResultLine> lines, DateOnly asOf) =>
        [.. lines
            .Where(l => l.TermStartsOn <= asOf)
            .GroupBy(l => l.SubjectId)
            .Select(g => g.MaxBy(l => l.TermStartsOn)!)];
}
