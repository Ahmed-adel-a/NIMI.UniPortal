using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Files;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.Modules.Assignments.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Assignments.Application.Submit;

/// <summary>
/// P8.3 — everything is checked before the file touches the disk: the student can see the assignment (R2), the
/// server clock is before the deadline (R3), it is their first submission (R4) and the file fits the assignment's
/// type and size rules (R5). Then the file is stored, a receipt number allocated (R6) and the submission recorded.
/// </summary>
internal sealed class SubmitAssignmentHandler(
    IAssignmentsDbContext db,
    AssignmentAccess access,
    IFileStorage storage,
    IReceiptAllocator receipts,
    IUsersApi users,
    TimeProvider clock) : IRequestHandler<SubmitAssignmentCommand, SubmissionReceiptDto>
{
    public async Task<SubmissionReceiptDto> Handle(SubmitAssignmentCommand command, CancellationToken cancellationToken)
    {
        if (!command.Caller.IsStudent)
            throw new ForbiddenException("Only students hand in assignments.");

        var (assignment, _) = await access.LoadVisibleAsync(command.AssignmentId, command.Caller, cancellationToken);

        var now = clock.GetUtcNow();
        if (!assignment.IsOpenAt(now))
            throw ValidationErrors.For("assignmentId", "The deadline has passed.");

        if (await db.Submissions.AnyAsync(s => s.AssignmentId == assignment.Id && s.StudentId == command.Caller.UserId, cancellationToken))
            throw new ConflictException("You have already submitted this assignment.");

        if (command.Content is null || string.IsNullOrWhiteSpace(command.FileName))
            throw ValidationErrors.For("file", "A file is required.");
        if (!assignment.Allows(command.FileName))
            throw ValidationErrors.For("file", $"Allowed file types: {assignment.AllowedExtensions}.");
        if (command.FileSize < 1 || command.FileSize > assignment.MaxFileMb * 1024L * 1024L)
            throw ValidationErrors.For("file", $"A file must be between 1 byte and {assignment.MaxFileMb} MB.");

        var student = await users.GetUserAsync(command.Caller.UserId, cancellationToken);
        var stored = await storage.SaveAsync(
            command.Content, command.FileName, command.ContentType ?? "application/octet-stream", cancellationToken);
        var sequence = await receipts.NextAsync(now.Year, cancellationToken);

        var submission = Submission.Create(
            assignment,
            command.Caller.UserId,
            student?.FullName ?? string.Empty,
            student?.StudentCode,
            stored.Key,
            command.FileName,
            stored.ContentType,
            stored.SizeBytes,
            SubmissionReceipt.Format(now.Year, sequence),
            now);
        db.Submissions.Add(submission);

        return new SubmissionReceiptDto(submission.Id, submission.ReceiptNumber, submission.SubmittedAt, submission.Status);
    }
}
