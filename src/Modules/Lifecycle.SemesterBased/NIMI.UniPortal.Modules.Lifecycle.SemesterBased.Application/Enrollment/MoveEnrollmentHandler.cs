using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Enrollment;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Enrollment;

internal sealed class MoveEnrollmentHandler(ISemesterDbContext db, ISchedulingApi scheduling)
    : IRequestHandler<MoveEnrollmentCommand, TermEnrollmentDto>
{
    public async Task<TermEnrollmentDto> Handle(MoveEnrollmentCommand command, CancellationToken cancellationToken)
    {
        var enrollment = await db.TermEnrollments.SingleOrDefaultAsync(e => e.Id == command.EnrollmentId, cancellationToken)
            ?? throw new NotFoundException($"No enrollment with id '{command.EnrollmentId}'.");
        if (enrollment.Status != EnrollmentStatus.Enrolled)
            throw new ConflictException("Only an enrollment without a result can change group.");

        var group = await scheduling.GetGroupAsync(command.SubjectGroupId, cancellationToken);
        if (group is null || !group.IsActive || group.SubjectId != enrollment.SubjectId || group.TermId != enrollment.TermId)
            throw ValidationErrors.For("subjectGroupId", "Choose an active group of the same subject and term.");

        if (group.Id != enrollment.SubjectGroupId)
        {
            var members = await db.TermEnrollments.CountAsync(
                e => e.SubjectGroupId == group.Id && e.Status != EnrollmentStatus.Withdrawn, cancellationToken);
            if (members >= group.Capacity)
                throw new ConflictException("The group is full.");
            enrollment.MoveTo(group.Id);
        }

        return TermEnrollmentDto.From(enrollment);
    }
}
