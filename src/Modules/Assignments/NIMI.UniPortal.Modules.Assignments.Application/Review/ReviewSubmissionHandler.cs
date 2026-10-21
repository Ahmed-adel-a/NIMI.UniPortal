using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.Modules.Assignments.Domain;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Assignments.Application.Review;

/// <summary>
/// P8.5 — only the subject's teaching staff review (R1), once (R7, 409 on a second review); the grade stays within the
/// assignment's maximum and a rejection needs a reason. The student is notified after the commit (R8).
/// </summary>
internal sealed class ReviewSubmissionHandler(
    IAssignmentsDbContext db, AssignmentAccess access, IEventCollector events, TimeProvider clock)
    : IRequestHandler<ReviewSubmissionCommand, SubmissionDto>
{
    public async Task<SubmissionDto> Handle(ReviewSubmissionCommand command, CancellationToken cancellationToken)
    {
        var submission = await db.Submissions.SingleOrDefaultAsync(s => s.Id == command.SubmissionId, cancellationToken)
            ?? throw new NotFoundException("Submission not found.");
        var (assignment, _) = await access.LoadForTeacherAsync(submission.AssignmentId, command.Caller, cancellationToken);

        if (submission.Status != SubmissionStatus.Submitted)
            throw new ConflictException("This submission has already been reviewed.");

        var now = clock.GetUtcNow();
        switch (command.Decision?.Trim().ToLowerInvariant())
        {
            case "approve":
                if (command.Grade is not { } grade)
                    throw ValidationErrors.For("grade", "A grade is required to approve.");
                submission.Approve(grade, assignment.MaxGrade, command.Feedback, command.Caller.UserId, now);
                break;
            case "reject":
                submission.Reject(command.RejectionReason ?? string.Empty, command.Caller.UserId, now);
                break;
            default:
                throw ValidationErrors.For("decision", "Decision must be approve or reject.");
        }

        var approved = submission.Status == SubmissionStatus.Approved;
        events.Add(new NotificationRequested(
            Guid.CreateVersion7(),
            now,
            [submission.StudentId],
            "assignment",
            Clip(approved ? $"Graded: {assignment.Title}" : $"Returned: {assignment.Title}", 200),
            Clip(approved ? $"تم تقييم: {assignment.Title}" : $"تم رفض: {assignment.Title}", 200),
            Clip(approved ? $"Your grade: {submission.Grade:0.##} / {assignment.MaxGrade:0.##}." : $"Reason: {submission.Feedback}", 2000),
            Clip(approved ? $"درجتك: {submission.Grade:0.##} من {assignment.MaxGrade:0.##}." : $"السبب: {submission.Feedback}", 2000),
            "/submissions/mine"));

        return SubmissionDto.From(submission);
    }

    private static string Clip(string text, int max) => text.Length <= max ? text : text[..(max - 3)] + "...";
}
