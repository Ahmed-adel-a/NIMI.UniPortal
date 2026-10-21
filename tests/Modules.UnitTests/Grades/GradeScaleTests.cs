// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Grades;

/// <summary>R5 — the letter and points come from the band with the highest minimum score not above the score.</summary>
public sealed class GradeScaleTests
{
    private static readonly GradeScale Egyptian = new(
    [
        new GradeBand("A+", 97m, 4.0m), new GradeBand("A", 93m, 4.0m), new GradeBand("A-", 89m, 3.7m),
        new GradeBand("B+", 84m, 3.3m), new GradeBand("B", 80m, 3.0m), new GradeBand("B-", 76m, 2.7m),
        new GradeBand("C+", 73m, 2.3m), new GradeBand("C", 70m, 2.0m), new GradeBand("C-", 67m, 1.7m),
        new GradeBand("D+", 64m, 1.3m), new GradeBand("D", 60m, 1.0m), new GradeBand("F", 0m, 0.0m),
    ]);

    [Theory]
    [InlineData(100, "A+", 4.0)]
    [InlineData(97, "A+", 4.0)]
    [InlineData(96.99, "A", 4.0)]
    [InlineData(89, "A-", 3.7)]
    [InlineData(84.5, "B+", 3.3)]
    [InlineData(72, "C", 2.0)]
    [InlineData(60, "D", 1.0)]
    [InlineData(59.99, "F", 0.0)]
    [InlineData(0, "F", 0.0)]
    public void The_highest_band_reached_wins(decimal score, string grade, decimal points)
    {
        var band = Egyptian.Resolve(score);

        Assert.Equal(grade, band.Grade);
        Assert.Equal(points, band.Points);
    }

    [Fact]
    public void Band_order_in_the_settings_does_not_matter()
    {
        var shuffled = new GradeScale([new GradeBand("F", 0m, 0m), new GradeBand("A", 90m, 4m), new GradeBand("B", 80m, 3m)]);

        Assert.Equal("B", shuffled.Resolve(85m).Grade);
        Assert.Equal("A", shuffled.Resolve(90m).Grade);
        Assert.Equal("F", shuffled.Resolve(79.99m).Grade);
    }

    [Fact]
    public void A_score_below_every_band_gets_the_lowest_band()
    {
        var noZero = new GradeScale([new GradeBand("P", 50m, 2m), new GradeBand("E", 30m, 0m)]);

        Assert.Equal("E", noZero.Resolve(10m).Grade);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Scores_outside_0_to_100_are_rejected(decimal score) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Egyptian.Resolve(score));

    [Fact]
    public void A_scale_needs_at_least_one_band() =>
        Assert.Throws<ArgumentException>(() => new GradeScale([]));
}
