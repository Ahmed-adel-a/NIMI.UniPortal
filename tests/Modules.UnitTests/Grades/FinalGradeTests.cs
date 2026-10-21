// ACCEPTANCE TEST — written by the consultant for phase P09. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Grades.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Grades;

/// <summary>D3 — draft → confirmed → published; published grades change only through an audited correction (R4).</summary>
public sealed class FinalGradeTests
{
    private static readonly DateTimeOffset Now = new(2027, 1, 20, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Teacher = Guid.CreateVersion7();
    private static readonly Guid Admin = Guid.CreateVersion7();
    private static readonly GradeBand B = new("B", 80m, 3.0m);
    private static readonly GradeBand A = new("A", 90m, 4.0m);

    [Fact]
    public void A_draft_snapshots_score_letter_points_and_credits()
    {
        var grade = Draft(85m, B);

        Assert.NotEqual(Guid.Empty, grade.Id);
        Assert.Equal(GradeStatus.Draft, grade.Status);
        Assert.Equal(85m, grade.Score);
        Assert.Equal("B", grade.Grade);
        Assert.Equal(3.0m, grade.GradePoints);
        Assert.Equal(3, grade.CreditHours);
        Assert.Equal(Teacher, grade.DraftedBy);
        Assert.True(grade.Passed);
    }

    [Fact]
    public void Zero_points_is_a_fail() => Assert.False(Draft(20m, new GradeBand("F", 0m, 0m)).Passed);

    [Fact]
    public void A_draft_can_be_redrafted()
    {
        var grade = Draft(85m, B);

        grade.Redraft(92m, A, Teacher, Now.AddHours(1));

        Assert.Equal(92m, grade.Score);
        Assert.Equal("A", grade.Grade);
        Assert.Equal(GradeStatus.Draft, grade.Status);
    }

    [Fact]
    public void Confirm_then_publish_records_who_and_when()
    {
        var grade = Draft(85m, B);

        grade.Confirm(Admin, Now.AddDays(1));
        grade.Publish(Admin, Now.AddDays(2));

        Assert.Equal(GradeStatus.Published, grade.Status);
        Assert.Equal(Admin, grade.ConfirmedBy);
        Assert.Equal(Now.AddDays(1), grade.ConfirmedAt);
        Assert.Equal(Admin, grade.PublishedBy);
        Assert.Equal(Now.AddDays(2), grade.PublishedAt);
    }

    [Fact]
    public void The_workflow_only_moves_forward()
    {
        var grade = Draft(85m, B);
        Assert.Throws<InvalidOperationException>(() => grade.Publish(Admin, Now));      // not confirmed yet

        grade.Confirm(Admin, Now);
        Assert.Throws<InvalidOperationException>(() => grade.Redraft(90m, A, Teacher, Now));
        Assert.Throws<InvalidOperationException>(() => grade.Confirm(Admin, Now));
    }

    [Fact]
    public void A_correction_changes_a_published_grade_and_returns_the_audit_row()
    {
        var grade = Draft(85m, B);
        grade.Confirm(Admin, Now);
        grade.Publish(Admin, Now);

        var correction = grade.Correct(92m, A, "Re-marked", Admin, Now.AddDays(3));

        Assert.Equal(92m, grade.Score);
        Assert.Equal("A", grade.Grade);
        Assert.Equal(4.0m, grade.GradePoints);
        Assert.Equal(GradeStatus.Published, grade.Status);
        Assert.Equal(grade.Id, correction.FinalGradeId);
        Assert.Equal(85m, correction.OldScore);
        Assert.Equal(92m, correction.NewScore);
        Assert.Equal("B", correction.OldGrade);
        Assert.Equal("A", correction.NewGrade);
        Assert.Equal("Re-marked", correction.Reason);
        Assert.Equal(Admin, correction.CorrectedBy);
    }

    [Fact]
    public void Only_published_grades_are_corrected_and_always_with_a_reason()
    {
        var draft = Draft(85m, B);
        Assert.Throws<InvalidOperationException>(() => draft.Correct(92m, A, "Early", Admin, Now));

        draft.Confirm(Admin, Now);
        draft.Publish(Admin, Now);
        Assert.Throws<ArgumentException>(() => draft.Correct(92m, A, " ", Admin, Now));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.5)]
    [InlineData(80.123)]
    public void Scores_are_0_to_100_with_two_decimals(decimal score) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Draft(score, B));

    private static FinalGrade Draft(decimal score, GradeBand band) =>
        FinalGrade.Draft(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            creditHours: 3, score, band, Teacher, Now);
}
