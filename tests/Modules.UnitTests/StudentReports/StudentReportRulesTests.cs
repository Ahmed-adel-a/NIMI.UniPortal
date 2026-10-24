using NIMI.UniPortal.Modules.StudentReports.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.StudentReports;

/// <summary>Decision ROLES2: a report is answered at most once and closed at most once; texts are required and bounded.</summary>
public sealed class StudentReportRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Student = Guid.CreateVersion7();
    private static readonly Guid Department = Guid.CreateVersion7();
    private static readonly Guid Head = Guid.CreateVersion7();

    [Fact]
    public void A_new_report_is_open_with_trimmed_texts()
    {
        var report = StudentReport.Submit(Student, Department, ReportCategory.Academic, "  Lab broken ", " Please fix. ", Now);

        Assert.Equal(ReportStatus.Open, report.Status);
        Assert.Equal("Lab broken", report.Title);
        Assert.Equal("Please fix.", report.Message);
        Assert.Null(report.Reply);
    }

    [Theory]
    [InlineData("", "message", "title")]
    [InlineData("title", " ", "message")]
    public void Title_and_message_are_required(string title, string message, string field) =>
        Assert.Equal(field, Assert.Throws<ArgumentException>(() =>
            StudentReport.Submit(Student, Department, ReportCategory.Other, title, message, Now)).ParamName);

    [Fact]
    public void Answering_records_the_reply_once()
    {
        var report = StudentReport.Submit(Student, Department, ReportCategory.Other, "Title", "Message", Now);

        report.Answer(Head, "Done.", Now.AddHours(1));

        Assert.Equal(ReportStatus.Answered, report.Status);
        Assert.Equal("Done.", report.Reply);
        Assert.Equal(Head, report.AnsweredBy);
        Assert.False(report.CanAnswer);
        Assert.Throws<InvalidOperationException>(() => report.Answer(Head, "Again.", Now.AddHours(2)));
    }

    [Fact]
    public void Closing_ends_the_report_from_open_or_answered()
    {
        var open = StudentReport.Submit(Student, Department, ReportCategory.Other, "Title", "Message", Now);
        open.Close(Head, Now.AddHours(1));
        Assert.Equal(ReportStatus.Closed, open.Status);
        Assert.False(open.CanAnswer);
        Assert.False(open.CanClose);
        Assert.Throws<InvalidOperationException>(() => open.Close(Head, Now.AddHours(2)));

        var answered = StudentReport.Submit(Student, Department, ReportCategory.Other, "Title", "Message", Now);
        answered.Answer(Head, "Reply.", Now.AddHours(1));
        answered.Close(Head, Now.AddHours(2));
        Assert.Equal(ReportStatus.Closed, answered.Status);
        Assert.Equal("Reply.", answered.Reply);
    }
}
