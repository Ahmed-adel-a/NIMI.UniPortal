using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Platform.Application.Abstractions;
using NIMI.UniPortal.Modules.Platform.Domain.AcademicSystems;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;

internal sealed class SetAcademicSystemHandler(
    IPlatformDbContext db,
    AcademicSystemResolver resolver,
    AcademicSystemsOverview overview,
    IUsersApi users,
    IAcademicLifecycle lifecycle,
    IEventCollector events,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<SetAcademicSystemCommand, AcademicSystemsOverviewDto>
{
    public async Task<AcademicSystemsOverviewDto> Handle(SetAcademicSystemCommand command, CancellationToken cancellationToken)
    {
        if (command.DepartmentId is { } departmentId && await users.GetDepartmentAsync(departmentId, cancellationToken) is null)
            throw new NotFoundException($"No department with id '{departmentId}'.");
        if (command.FacultyId is { } facultyId && await users.GetFacultyAsync(facultyId, cancellationToken) is null)
            throw new NotFoundException($"No faculty with id '{facultyId}'.");
        AcademicSystem? system = AcademicSystemNames.TryParse(command.AcademicSystem, out var parsed) ? parsed : null;

        var now = clock.GetUtcNow();
        var rules = await db.AcademicSystemRules.ToListAsync(cancellationToken);
        var existing = rules.SingleOrDefault(r => r.FacultyId == command.FacultyId && r.DepartmentId == command.DepartmentId);

        // The rules as they will be, to see which departments change system.
        var after = rules.Where(r => r != existing).ToList();
        if (system is { } next)
        {
            after.Add(command.DepartmentId is { } d ? AcademicSystemRule.ForDepartment(d, next, now)
                : command.FacultyId is { } f ? AcademicSystemRule.ForFaculty(f, next, now)
                : AcademicSystemRule.ForUniversity(next, now));
        }

        foreach (var department in await users.GetDepartmentsAsync(null, cancellationToken))
        {
            var before = AcademicSystemRules.Resolve(rules, department.FacultyId, department.Id, resolver.Fallback).System;
            var then = AcademicSystemRules.Resolve(after, department.FacultyId, department.Id, resolver.Fallback).System;
            if (before == then)
                continue;
            var students = await users.GetActiveStudentIdsAsync(null, department.Id, cancellationToken);
            if (students.Count > 0 &&
                await lifecycle.HasStudentRecordsAsync(AcademicSystemNames.Name(before), students, cancellationToken))
            {
                throw new ConflictException(
                    $"Students of {department.Code} already have records in {AcademicSystemNames.Name(before)}; " +
                    "its academic system can't change while they study under it.");
            }
        }

        if (system is { } value)
        {
            if (existing is not null)
                existing.Change(value, now);
            else
                db.AcademicSystemRules.Add(after[^1]);
        }
        else if (existing is not null)
        {
            db.AcademicSystemRules.Remove(existing);
        }

        var scope = command.DepartmentId is not null ? "department" : command.FacultyId is not null ? "faculty" : "university";
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(), now, currentUser.UserId, AuditActions.AcademicSystemChanged, AuditEntities.AcademicSystemRule,
            command.DepartmentId ?? command.FacultyId ?? Guid.Empty,
            $"Academic system of the {scope} set to {command.AcademicSystem ?? "inherit"}."));

        return await overview.BuildAsync(after, cancellationToken);   // saved by the UnitOfWork
    }
}
