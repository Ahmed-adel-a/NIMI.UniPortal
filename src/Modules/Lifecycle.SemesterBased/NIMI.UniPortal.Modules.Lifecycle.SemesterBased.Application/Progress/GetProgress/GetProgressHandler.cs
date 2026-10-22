using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Progress.GetProgress;

internal sealed class GetProgressHandler(IAcademicLifecycle lifecycle, ISchedulingApi scheduling)
    : IRequestHandler<GetProgressQuery, StudentProgressDto>
{
    public async Task<StudentProgressDto> Handle(
        GetProgressQuery query, CancellationToken cancellationToken)
    {
        if (!query.IsStudent)
            throw new ForbiddenException("Only students have progress.");

        var progress = await lifecycle.GetProgressAsync(query.StudentId, cancellationToken);

        // The seam reports ids only; the API needs code and both names. Codes come from Scheduling, never copied here.
        var outstanding = new List<OutstandingRequirementDto>(
            progress.OutstandingRequiredSubjectIds.Count);
        foreach (var subjectId in progress.OutstandingRequiredSubjectIds)
        {
            var subject = await scheduling.GetSubjectAsync(subjectId, cancellationToken);
            if (subject is not null)
                outstanding.Add(new OutstandingRequirementDto(
                    subject.Id, subject.Code, subject.Name, subject.NameAr));
        }

        return new StudentProgressDto(
            progress.StudentId,
            progress.CreditsEarned,
            progress.CreditsInProgress,
            progress.CumulativeGpa,
            outstanding);
    }
}
