// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Grades;

/// <summary>P09 §6 — credit-weighted GPA, 3 decimals rounded half away from zero; cumulative counts the latest attempt only.</summary>
public sealed class GpaTests
{
    [Fact]
    public void Gpa_is_weighted_by_credit_hours() =>
        Assert.Equal(3.4m, Gpa.Calculate([new GpaEntry(3, 3.0m), new GpaEntry(2, 4.0m)]));

    [Fact]
    public void Zero_credit_subjects_do_not_count() =>
        Assert.Equal(3.0m, Gpa.Calculate([new GpaEntry(3, 3.0m), new GpaEntry(0, 0.0m)]));

    [Fact]
    public void No_credits_means_no_gpa()
    {
        Assert.Null(Gpa.Calculate([]));
        Assert.Null(Gpa.Calculate([new GpaEntry(0, 4.0m)]));
    }

    [Fact]
    public void Gpa_is_rounded_to_three_decimals_half_away_from_zero()
    {
        // (17 × 3.0 + 3 × 3.67) / 20 = 62.01 / 20 = 3.1005 → 3.101 (banker's rounding would give 3.100)
        Assert.Equal(3.101m, Gpa.Calculate([new GpaEntry(17, 3.0m), new GpaEntry(3, 3.67m)]));
        Assert.Equal(3.333m, Gpa.Calculate([new GpaEntry(3, 4.0m), new GpaEntry(3, 3.0m), new GpaEntry(3, 3.0m)]));
    }

    [Fact]
    public void Cumulative_gpa_counts_only_the_latest_attempt_of_each_subject()
    {
        var algorithms = Guid.CreateVersion7();
        var networks = Guid.CreateVersion7();
        var first = new DateOnly(2026, 9, 19);
        var second = new DateOnly(2027, 2, 6);

        var cumulative = Gpa.Cumulative(
        [
            new GpaAttempt(networks, second, 3, 3.0m),       // retake passed
            new GpaAttempt(algorithms, first, 3, 4.0m),
            new GpaAttempt(networks, first, 3, 0.0m),        // first attempt failed — replaced
        ]);

        Assert.Equal(3.5m, cumulative);
    }

    [Fact]
    public void Cumulative_gpa_of_nothing_is_null() => Assert.Null(Gpa.Cumulative([]));
}
