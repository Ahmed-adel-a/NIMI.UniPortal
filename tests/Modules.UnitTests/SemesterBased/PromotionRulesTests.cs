// ACCEPTANCE TEST — written by the consultant for phase P4.B. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Promotion;

namespace NIMI.UniPortal.Modules.UnitTests.SemesterBased;

/// <summary>
/// Decision SEMPROMO — the year-end rule of the semester-based (فرق) system. Defaults: up to 3 carried subjects,
/// no stage gates, at most 2 years in one level (executive regulations art. 80), fees must be paid.
/// </summary>
public sealed class PromotionRulesTests
{
    private static readonly PromotionRuleSet Defaults = PromotionPolicy.DefaultRules;

    private static PromotionOutcome Decide(
        int level, int failed, int finalLevel = 4, int yearsInLevel = 1, bool feesOutstanding = false, PromotionRuleSet? rules = null) =>
        PromotionRules.Decide(new PromotionInput(level, finalLevel, yearsInLevel, failed, feesOutstanding), rules ?? Defaults);

    [Fact]
    public void The_defaults_are_three_carried_subjects_two_years_per_level_and_paid_fees()
    {
        Assert.Equal(3, Defaults.MaxCarriedSubjects);
        Assert.Empty(Defaults.GateLevels);
        Assert.Equal(2, Defaults.MaxYearsPerLevel);
        Assert.True(Defaults.RequireFeesPaid);
    }

    [Fact]
    public void Passing_everything_moves_up_one_level()
    {
        var outcome = Decide(level: 1, failed: 0);

        Assert.Equal(PromotionResult.Promoted, outcome.Result);
        Assert.Equal(2, outcome.ToLevel);
    }

    [Fact]
    public void Up_to_the_maximum_failed_subjects_move_up_carrying_them()
    {
        var outcome = Decide(level: 2, failed: 3);

        Assert.Equal(PromotionResult.PromotedWithCarried, outcome.Result);
        Assert.Equal(3, outcome.ToLevel);
    }

    [Fact]
    public void More_failed_subjects_than_allowed_repeat_the_level()
    {
        var outcome = Decide(level: 2, failed: 4);

        Assert.Equal(PromotionResult.Repeat, outcome.Result);
        Assert.Equal(2, outcome.ToLevel);
    }

    [Fact]
    public void After_the_maximum_years_in_a_level_the_student_sits_failed_subjects_from_outside()
    {
        var outcome = Decide(level: 2, failed: 4, yearsInLevel: 2);

        Assert.Equal(PromotionResult.External, outcome.Result);
        Assert.Equal(2, outcome.ToLevel);
    }

    [Fact]
    public void A_stage_gate_is_entered_only_with_every_lower_subject_passed()
    {
        var twoPlusTwo = Defaults with { GateLevels = [3] };

        Assert.Equal(PromotionResult.Repeat, Decide(level: 2, failed: 1, rules: twoPlusTwo).Result);
        Assert.Equal(PromotionResult.Promoted, Decide(level: 2, failed: 0, rules: twoPlusTwo).Result);
        Assert.Equal(PromotionResult.PromotedWithCarried, Decide(level: 1, failed: 1, rules: twoPlusTwo).Result);
    }

    [Fact]
    public void The_final_level_graduates_only_with_everything_passed()
    {
        Assert.Equal(PromotionResult.Graduated, Decide(level: 4, failed: 0).Result);

        var failedOne = Decide(level: 4, failed: 1);
        Assert.Equal(PromotionResult.Repeat, failedOne.Result);
        Assert.Equal(4, failedOne.ToLevel);
    }

    [Fact]
    public void Outstanding_fees_hold_the_decision_when_the_policy_requires_paid_fees()
    {
        var held = Decide(level: 1, failed: 0, feesOutstanding: true);
        Assert.Equal(PromotionResult.Held, held.Result);
        Assert.Equal(1, held.ToLevel);

        var ignored = Decide(level: 1, failed: 0, feesOutstanding: true, rules: Defaults with { RequireFeesPaid = false });
        Assert.Equal(PromotionResult.Promoted, ignored.Result);
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(11, 2)]
    [InlineData(3, 0)]
    [InlineData(3, 6)]
    public void Out_of_range_policy_numbers_are_rejected(int maxCarried, int maxYears)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PromotionPolicy.CreateDefault(maxCarried, [], maxYears, true, DateTimeOffset.UnixEpoch));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void Gate_levels_must_be_between_two_and_seven(int gate)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PromotionPolicy.CreateDefault(3, [gate], 2, true, DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void A_faculty_policy_keeps_its_faculty_and_sorted_distinct_gates()
    {
        var faculty = Guid.CreateVersion7();

        var policy = PromotionPolicy.CreateForFaculty(faculty, 2, [5, 3, 3], 2, false, DateTimeOffset.UnixEpoch);

        Assert.Equal(faculty, policy.FacultyId);
        Assert.Equal([3, 5], policy.GateLevels);
        Assert.Equal(7, policy.Id.Version);
    }
}
