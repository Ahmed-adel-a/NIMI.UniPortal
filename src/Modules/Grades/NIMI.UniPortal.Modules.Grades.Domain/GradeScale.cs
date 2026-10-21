namespace NIMI.UniPortal.Modules.Grades.Domain;

/// <summary>
/// Rule R5 — a faculty's effective grading scale. A score earns the band with the highest minimum not above it;
/// a score below every band earns the lowest band. Band order in the settings does not matter.
/// </summary>
public sealed class GradeScale
{
    private readonly GradeBand[] bands;

    public GradeScale(IEnumerable<GradeBand> bands)
    {
        this.bands = [.. bands.OrderByDescending(b => b.MinScore)];
        if (this.bands.Length == 0)
            throw new ArgumentException("A grading scale needs at least one band.", nameof(bands));
    }

    public GradeBand Resolve(decimal score)
    {
        if (score is < 0m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(score), score, "Between 0 and 100.");

        return bands.FirstOrDefault(b => b.MinScore <= score) ?? bands[^1];
    }
}
