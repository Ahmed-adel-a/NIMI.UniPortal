namespace NIMI.UniPortal.Modules.Grades.Domain;

/// <summary>
/// P09 §6 — GPA = Σ(points × credits) / Σ(credits), rounded to 3 decimals half away from zero. Zero-credit subjects
/// (military education) carry no weight; no credits at all means no GPA.
/// </summary>
public static class Gpa
{
    public static decimal? Calculate(IEnumerable<GpaEntry> entries)
    {
        var credits = 0;
        var weighted = 0m;
        foreach (var entry in entries)
        {
            credits += entry.CreditHours;
            weighted += entry.GradePoints * entry.CreditHours;
        }

        return credits == 0 ? null : Math.Round(weighted / credits, 3, MidpointRounding.AwayFromZero);
    }

    public static decimal? Cumulative(IEnumerable<GpaAttempt> attempts) =>
        Calculate(attempts
            .GroupBy(a => a.SubjectId)
            .Select(g => g.MaxBy(a => a.TermStartsOn)!)
            .Select(a => new GpaEntry(a.CreditHours, a.GradePoints)));
}
