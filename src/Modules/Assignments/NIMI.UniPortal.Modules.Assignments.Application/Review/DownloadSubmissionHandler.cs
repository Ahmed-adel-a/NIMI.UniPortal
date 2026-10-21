using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Files;
using NIMI.UniPortal.Modules.Assignments.Application.Abstractions;
using NIMI.UniPortal.Modules.Assignments.Application.Common;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Application.Review;

internal sealed class DownloadSubmissionHandler(
    IAssignmentsDbContext db, CourseAccess course, IFileStorage storage)
    : IRequestHandler<DownloadSubmissionQuery, SubmissionFileDto>
{
    public async Task<SubmissionFileDto> Handle(DownloadSubmissionQuery query, CancellationToken cancellationToken)
    {
        var submission = await db.Submissions.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == query.SubmissionId, cancellationToken)
            ?? throw new NotFoundException("Submission not found.");

        if (submission.StudentId != query.Caller.UserId)
        {
            var assignment = await db.Assignments.AsNoTracking()
                .SingleAsync(a => a.Id == submission.AssignmentId, cancellationToken);
            var teaches = query.Caller.IsProfessor &&
                          await course.TeachesSubjectAsync(query.Caller.UserId, assignment.SubjectId, assignment.TermId, cancellationToken);
            if (!teaches)
                throw new NotFoundException("Submission not found.");
        }

        try
        {
            return new SubmissionFileDto(
                await storage.OpenReadAsync(submission.StorageKey, cancellationToken), submission.FileName, submission.ContentType);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("The submitted file is missing.");
        }
    }
}
