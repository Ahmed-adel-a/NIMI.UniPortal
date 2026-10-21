using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.RegisterForSubject;

internal sealed class RegisterForSubjectHandler(
    ICreditHoursDbContext db,
    ISeatCounter seats,
    ISchedulingApi scheduling,
    IUsersApi users,
    TimeProvider clock)
    : IRequestHandler<RegisterForSubjectCommand, RegistrationDto>
{
    private const string Field = "subjectGroupId";
    private const string MilitaryEducation = "military_education";
    private const string FullMessage = "The group is full.";

    public async Task<RegistrationDto> Handle(
        RegisterForSubjectCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsStudent)
            throw new ForbiddenException("Only students can register.");

        var student = await users.GetUserAsync(command.StudentId, cancellationToken)
            ?? throw new NotFoundException("The signed-in student was not found.");

        var now = clock.GetUtcNow();
        var term = await scheduling.GetCurrentTermAsync(cancellationToken)
            ?? throw ValidationErrors.For(Field, "Registration is closed.");

        var group = await scheduling.GetGroupAsync(command.SubjectGroupId, cancellationToken)
            ?? throw ValidationErrors.For(Field, "Unknown group.");

        var subject = await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken)
            ?? throw ValidationErrors.For(Field, "Unknown subject.");

        var registrations = await db.CourseRegistrations.AsNoTracking()
            .Where(r => r.StudentId == student.Id &&
                        r.TermId == term.Id &&
                        r.Status != RegistrationStatus.Dropped)
            .OrderBy(r => r.RegisteredAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        // The phase requires 409 for a second live registration of the same subject, before the rule list (R4).
        if (registrations.Any(r => r.SubjectId == subject.Id))
            throw new ConflictException("Already registered for this subject.");

        var window = await db.RegistrationWindows.AsNoTracking()
            .FirstOrDefaultAsync(w => w.TermId == term.Id, cancellationToken);

        var currentCredits = registrations.Sum(r => r.CreditHours);
        var registeredGroupIds = registrations.Select(r => r.SubjectGroupId).ToList();

        var missingPrerequisiteCodes = await MissingPrerequisiteCodesAsync(
            student.Id, subject.Id, cancellationToken);

        var clashingGroups = new List<string>();
        var otherGroupIds = registeredGroupIds.Where(id => id != group.Id).ToList();
        if (otherGroupIds.Count > 0)
        {
            var conflicts = await scheduling.FindTimeConflictsAsync(
                group.Id, otherGroupIds, cancellationToken);

            foreach (var conflictId in conflicts)
            {
                var conflictGroup = await scheduling.GetGroupAsync(conflictId, cancellationToken);
                if (conflictGroup is null)
                    continue;

                var conflictSubject = await scheduling.GetSubjectAsync(
                    conflictGroup.SubjectId, cancellationToken);
                if (conflictSubject is null)
                    continue;

                clashingGroups.Add($"{conflictSubject.Code} group {conflictGroup.GroupNumber}");
            }
        }

        var context = new RegistrationContext(
            Now: now,
            Window: window,
            GroupIsOffered: group.IsActive && group.TermId == term.Id,
            AlreadyRegisteredForSubject: false,
            IsMilitaryEducation: subject.Category == MilitaryEducation,
            TargetDepartmentId: group.TargetDepartmentId,
            StudentDepartmentId: student.DepartmentId,
            MissingPrerequisiteCodes: missingPrerequisiteCodes,
            CurrentCredits: currentCredits,
            SubjectCredits: subject.CreditHours,
            ClashingGroups: clashingGroups);

        RegistrationPolicy.EnsureCanRegister(context);

        await seats.BeginAsync(cancellationToken);
        try
        {
            // Rule R8: the condition and the increment are one statement, so two racers can never both pass it.
            if (!await seats.TryTakeAsync(group.Id, group.Capacity, cancellationToken))
                throw new ConflictException(FullMessage);

            var registration = CourseRegistration.Register(
                student.Id, term.Id, subject.Id, group.Id, subject.CreditHours, now);
            db.CourseRegistrations.Add(registration);

            return new RegistrationDto(
                registration.Id,
                subject.Id,
                subject.Code,
                group.Id,
                group.GroupNumber,
                registration.CreditHours,
                registration.Status,
                registration.RegisteredAt);
        }
        catch
        {
            await seats.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<IReadOnlyList<string>> MissingPrerequisiteCodesAsync(
        Guid studentId, Guid subjectId, CancellationToken cancellationToken)
    {
        var prerequisiteIds = await db.SubjectPrerequisites.AsNoTracking()
            .Where(p => p.SubjectId == subjectId)
            .Select(p => p.PrerequisiteSubjectId)
            .ToListAsync(cancellationToken);

        if (prerequisiteIds.Count == 0)
            return [];

        var passed = (await db.CreditLedger.AsNoTracking()
            .Where(e => e.StudentId == studentId &&
                        e.Passed &&
                        prerequisiteIds.Contains(e.SubjectId))
            .Select(e => e.SubjectId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var codes = new List<string>();
        foreach (var prerequisiteId in prerequisiteIds.Where(id => !passed.Contains(id)))
        {
            var prerequisite = await scheduling.GetSubjectAsync(prerequisiteId, cancellationToken);
            if (prerequisite is not null)
                codes.Add(prerequisite.Code);
        }

        codes.Sort(StringComparer.Ordinal);
        return codes;
    }
}
