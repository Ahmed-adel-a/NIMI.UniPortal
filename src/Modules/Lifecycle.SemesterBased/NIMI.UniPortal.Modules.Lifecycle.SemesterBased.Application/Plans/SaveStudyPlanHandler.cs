using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Domain.Plans;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

internal sealed class SaveStudyPlanHandler(
    ISemesterDbContext db,
    IUsersApi users,
    ISchedulingApi scheduling,
    IEventCollector events,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<SaveStudyPlanCommand, StudyPlanDto>
{
    public async Task<StudyPlanDto> Handle(SaveStudyPlanCommand command, CancellationToken cancellationToken)
    {
        var department = await users.GetDepartmentAsync(command.DepartmentId, cancellationToken)
            ?? throw new NotFoundException($"No department with id '{command.DepartmentId}'.");

        var entries = command.Subjects ?? [];
        foreach (var entry in entries)
        {
            var subject = await scheduling.GetSubjectAsync(entry.SubjectId, cancellationToken);
            if (subject is null || !subject.IsActive)
                throw ValidationErrors.For("subjects", $"Unknown or inactive subject '{entry.SubjectId}'.");
        }

        var now = clock.GetUtcNow();
        var plan = await db.StudyPlans
            .Include(p => p.Subjects)
            .SingleOrDefaultAsync(p => p.DepartmentId == command.DepartmentId, cancellationToken);
        if (plan is null)
        {
            plan = StudyPlan.Create(command.DepartmentId, now);
            db.StudyPlans.Add(plan);
        }

        plan.Replace(command.Levels, [.. entries.Select(e => (e.Level, e.Term, e.SubjectId))], now);

        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(), now, currentUser.UserId, AuditActions.StudyPlanSaved, AuditEntities.Department, department.Id,
            $"Study plan of {department.Code} saved: {plan.Levels} levels, {entries.Count} subjects."));

        return await StudyPlanReader.ToDtoAsync(plan, scheduling, cancellationToken);
    }
}
