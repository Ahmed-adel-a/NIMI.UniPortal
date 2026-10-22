using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Standing;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

internal sealed class PlaceStudentHandler(
    ISemesterDbContext db, IUsersApi users, IEventCollector events, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<PlaceStudentCommand, StandingDto>
{
    public async Task<StandingDto> Handle(PlaceStudentCommand command, CancellationToken cancellationToken)
    {
        var placement = await users.GetStudentPlacementAsync(command.StudentId, cancellationToken)
            ?? throw new NotFoundException($"No student with id '{command.StudentId}'.");

        var plan = await db.StudyPlans.AsNoTracking()
            .SingleOrDefaultAsync(p => p.DepartmentId == placement.DepartmentId, cancellationToken)
            ?? throw ValidationErrors.For("studentId", "The student's department has no study plan yet.");
        if (command.Level > plan.Levels)
            throw ValidationErrors.For("level", $"The department's programme has {plan.Levels} levels.");

        var now = clock.GetUtcNow();
        var standing = await db.Standings.SingleOrDefaultAsync(s => s.StudentId == command.StudentId, cancellationToken);
        if (standing is null)
        {
            standing = StudentStanding.Place(command.StudentId, placement.DepartmentId, command.Level, now);
            db.Standings.Add(standing);
        }
        else
        {
            standing.MoveTo(placement.DepartmentId, command.Level, now);
        }

        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(), now, currentUser.UserId, AuditActions.StandingPlaced, AuditEntities.User, command.StudentId,
            $"Student placed in level {command.Level}."));

        return await StandingReader.ToDtoAsync(standing, users, cancellationToken);
    }
}
