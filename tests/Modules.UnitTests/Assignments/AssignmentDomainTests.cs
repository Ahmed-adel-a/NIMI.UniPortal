// ACCEPTANCE TEST — written by the consultant for phase P08. Do not modify; report problems instead.
using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.UnitTests.Assignments;

/// <summary>Assignments, submissions with receipts and the one-time review (rules R3–R7).</summary>
public sealed class AssignmentDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Due = Now.AddDays(7);

    [Fact]
    public void An_assignment_normalizes_its_allowed_extensions()
    {
        var assignment = NewAssignment(allowedExtensions: " PDF, .docx ,zip ");

        Assert.Equal(7, assignment.Id.Version);
        Assert.Equal("pdf,docx,zip", assignment.AllowedExtensions);
        Assert.Equal(["pdf", "docx", "zip"], assignment.AllowedExtensionList);
        Assert.True(assignment.Allows("Report.PDF"));
        Assert.False(assignment.Allows("report.exe"));
        Assert.True(assignment.IsOpenAt(Due.AddTicks(-1)));
        Assert.False(assignment.IsOpenAt(Due));
    }

    [Fact]
    public void Invalid_assignments_are_rejected_with_their_field_name()
    {
        AssertRejected("title", () => NewAssignment(title: " "));
        AssertRejected("dueAt", () => NewAssignment(dueAt: Now));
        AssertRejected("maxGrade", () => NewAssignment(maxGrade: 0m));
        AssertRejected("maxGrade", () => NewAssignment(maxGrade: 10.555m));
        AssertRejected("maxGrade", () => NewAssignment(maxGrade: 1000.01m));
        AssertRejected("allowedExtensions", () => NewAssignment(allowedExtensions: ""));
        AssertRejected("allowedExtensions", () => NewAssignment(allowedExtensions: "pdf,ex e"));
        AssertRejected("maxFileMb", () => NewAssignment(maxFileMb: 0));
        AssertRejected("maxFileMb", () => NewAssignment(maxFileMb: 51));
    }

    [Fact]
    public void Before_any_submission_everything_can_change_but_not_into_the_past()
    {
        var assignment = NewAssignment();

        assignment.Update("Lab 2 (revised)", "Use the new template", Due.AddDays(2), 20m, "pdf", 5, hasSubmissions: false, Now);

        Assert.Equal("Lab 2 (revised)", assignment.Title);
        Assert.Equal(20m, assignment.MaxGrade);
        Assert.Equal(["pdf"], assignment.AllowedExtensionList);
        AssertRejected("dueAt", () => assignment.Update("Lab 2", null, Now.AddMinutes(-1), 20m, "pdf", 5, false, Now));
    }

    [Fact]
    public void After_a_submission_only_the_text_and_a_later_due_date_may_change()
    {
        var assignment = NewAssignment();

        assignment.Update("Lab 2 — clarified", "See the FAQ", Due.AddDays(1), 10m, "pdf,docx,zip", 10, hasSubmissions: true, Now);

        Assert.Equal(Due.AddDays(1), assignment.DueAt);
        AssertRejected("dueAt", () => assignment.Update("Lab 2", null, Due, 10m, "pdf,docx,zip", 10, true, Now));
        AssertRejected("maxGrade", () => assignment.Update("Lab 2", null, Due.AddDays(1), 15m, "pdf,docx,zip", 10, true, Now));
        AssertRejected("allowedExtensions", () => assignment.Update("Lab 2", null, Due.AddDays(1), 10m, "pdf", 10, true, Now));
        AssertRejected("maxFileMb", () => assignment.Update("Lab 2", null, Due.AddDays(1), 10m, "pdf,docx,zip", 20, true, Now));
    }

    [Fact]
    public void An_assignment_with_submissions_cannot_be_deleted()
    {
        var assignment = NewAssignment();

        Assert.Throws<InvalidOperationException>(() => assignment.Delete(hasSubmissions: true, Now));
        assignment.Delete(hasSubmissions: false, Now);
        Assert.True(assignment.IsDeleted);
    }

    [Fact]
    public void A_submission_keeps_the_file_the_receipt_and_the_student_snapshot()
    {
        var assignment = NewAssignment();
        var student = Guid.CreateVersion7();

        var submission = Submit(assignment, student, "lab2.pdf", 4096);

        Assert.Equal(assignment.Id, submission.AssignmentId);
        Assert.Equal(student, submission.StudentId);
        Assert.Equal("أحمد علي", submission.StudentName);
        Assert.Equal("CS-2026-0001", submission.StudentCode);
        Assert.Equal("SUB-2026-000001", submission.ReceiptNumber);
        Assert.Equal(SubmissionStatus.Submitted, submission.Status);
        Assert.Equal(Now, submission.SubmittedAt);
    }

    [Fact]
    public void Submissions_respect_the_deadline_type_and_size()
    {
        var assignment = NewAssignment(maxFileMb: 1);

        Assert.Throws<InvalidOperationException>(() =>
            Submission.Create(assignment, Guid.CreateVersion7(), "A", null, "key", "lab.pdf", "application/pdf", 10, "SUB-2026-000002", Due));
        AssertRejected("file", () => Submit(assignment, Guid.CreateVersion7(), "lab.exe", 10));
        AssertRejected("file", () => Submit(assignment, Guid.CreateVersion7(), "lab.pdf", 0));
        AssertRejected("file", () => Submit(assignment, Guid.CreateVersion7(), "lab.pdf", 1024 * 1024 + 1));
    }

    [Fact]
    public void A_submission_is_reviewed_once()
    {
        var approved = Submit(NewAssignment(), Guid.CreateVersion7(), "lab.pdf", 10);
        var rejected = Submit(NewAssignment(), Guid.CreateVersion7(), "lab.pdf", 10);
        var reviewer = Guid.CreateVersion7();

        approved.Approve(8.5m, 10m, "Good work", reviewer, Now.AddDays(1));
        rejected.Reject("Wrong file uploaded", reviewer, Now.AddDays(1));

        Assert.Equal(SubmissionStatus.Approved, approved.Status);
        Assert.Equal(8.5m, approved.Grade);
        Assert.Equal("Good work", approved.Feedback);
        Assert.Equal(reviewer, approved.ReviewedBy);
        Assert.Equal(SubmissionStatus.Rejected, rejected.Status);
        Assert.Equal("Wrong file uploaded", rejected.Feedback);
        Assert.Null(rejected.Grade);
        Assert.Throws<InvalidOperationException>(() => approved.Approve(9m, 10m, null, reviewer, Now.AddDays(2)));
        Assert.Throws<InvalidOperationException>(() => rejected.Approve(9m, 10m, null, reviewer, Now.AddDays(2)));
    }

    [Fact]
    public void Review_values_are_checked()
    {
        var submission = Submit(NewAssignment(), Guid.CreateVersion7(), "lab.pdf", 10);

        AssertRejected("grade", () => submission.Approve(10.01m, 10m, null, Guid.CreateVersion7(), Now));
        AssertRejected("grade", () => submission.Approve(-1m, 10m, null, Guid.CreateVersion7(), Now));
        AssertRejected("rejectionReason", () => submission.Reject(" ", Guid.CreateVersion7(), Now));
    }

    [Theory]
    [InlineData(2026, 1, "SUB-2026-000001")]
    [InlineData(2027, 345, "SUB-2027-000345")]
    public void Receipts_are_numbered_per_year(int year, int sequence, string expected)
    {
        Assert.Equal(expected, SubmissionReceipt.Format(year, sequence));
        Assert.Equal("sequence", Assert.ThrowsAny<ArgumentException>(() => SubmissionReceipt.Format(year, 0)).ParamName);
    }

    private static Assignment NewAssignment(
        string title = "Lab 2", DateTimeOffset? dueAt = null, decimal maxGrade = 10m, string allowedExtensions = "pdf,docx,zip", int maxFileMb = 10) =>
        Assignment.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), null, Guid.CreateVersion7(), title, "Implement Dijkstra",
            dueAt ?? Due, maxGrade, allowedExtensions, maxFileMb, Now);

    private static Submission Submit(Assignment assignment, Guid student, string fileName, long size) =>
        Submission.Create(assignment, student, "أحمد علي", "CS-2026-0001", "2026/10/key", fileName, "application/pdf", size, "SUB-2026-000001", Now);

    private static void AssertRejected(string field, Action act) =>
        Assert.Equal(field, Assert.ThrowsAny<ArgumentException>(act).ParamName);
}
