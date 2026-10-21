using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateTeachingAssignment;

internal sealed class CreateTeachingAssignmentHandler(
    ISchedulingDbContext db,
    IUsersApi users,
    TimeProvider clock) : IRequestHandler<CreateTeachingAssignmentCommand, TeachingAssignmentDto>
{
    public async Task<TeachingAssignmentDto> Handle(
        CreateTeachingAssignmentCommand command, CancellationToken cancellationToken)
    {
        var staff = await users.GetUserAsync(command.StaffId, cancellationToken);
        if (staff is null || !staff.IsActive || staff.Role != PortalRoles.Professor)
            throw ValidationErrors.For(nameof(command.StaffId), "Must be an active professor.");

        var subject = await db.Subjects.FirstOrDefaultAsync(s => s.Id == command.SubjectId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.SubjectId), "Unknown subject.");

        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == command.TermId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.TermId), "Unknown term.");

        SubjectGroup? group = null;
        if (command.SubjectGroupId is { } groupId)
            group = await db.SubjectGroups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.SubjectGroupId), "Unknown group.");

        var assignment = TeachingAssignment.Create(
            command.StaffId, subject, term, group, command.Role, command.AssignedBy, clock.GetUtcNow());

        var duplicate = await db.TeachingAssignments.AnyAsync(a =>
            a.StaffId == command.StaffId &&
            a.SubjectId == command.SubjectId &&
            a.TermId == command.TermId &&
            a.SubjectGroupId == command.SubjectGroupId,
            cancellationToken);
        if (duplicate)
            throw new ConflictException("This staff member already has this teaching assignment.");

        db.TeachingAssignments.Add(assignment);

        return new TeachingAssignmentDto(
            assignment.Id,
            assignment.StaffId,
            staff.FullName,
            assignment.SubjectId,
            subject.Code,
            assignment.TermId,
            assignment.SubjectGroupId,
            group?.GroupNumber,
            assignment.Role,
            assignment.IsActive);
    }
}
