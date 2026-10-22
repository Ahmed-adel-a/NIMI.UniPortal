using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Common;
using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.AssignMilitaryEducation;

/// <summary>
/// P5.5 — Student Affairs assigns students to a military-education group, whatever the faculty policy says. Each
/// student gets the same pending registration and queued <see cref="MilitaryEducationRequested"/> as an opt-in
/// application; a professor/admin id is reported as <c>not_a_student</c> instead of failing the whole batch.
/// </summary>
internal sealed class AssignMilitaryEducationHandler(
    ICreditHoursDbContext db,
    ISeatCounter seats,
    ISchedulingApi scheduling,
    IUsersApi users,
    IEventCollector events,
    TimeProvider clock,
    CreditHoursStudents creditHours)
    : IRequestHandler<AssignMilitaryEducationCommand, MilitaryEducationAssignmentDto>
{
    private const string Field = "subjectGroupId";
    private const string MilitaryEducation = "military_education";
    private const string FullMessage = "The group is full.";

    public async Task<MilitaryEducationAssignmentDto> Handle(
        AssignMilitaryEducationCommand command, CancellationToken cancellationToken)
    {
        var term = await scheduling.GetCurrentTermAsync(cancellationToken)
            ?? throw ValidationErrors.For(Field, "There is no active term.");

        var group = await scheduling.GetGroupAsync(command.SubjectGroupId, cancellationToken);
        if (group is null || !group.IsActive || group.TermId != term.Id)
            throw ValidationErrors.For(Field, "This group is not offered in the active term.");

        var subject = await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken);
        if (subject is null || subject.Category != MilitaryEducation)
            throw ValidationErrors.For(Field, "This group is not a military-education group.");

        var students = new Dictionary<Guid, UserRef?>();
        var onCreditHours = new HashSet<Guid>();
        foreach (var studentId in command.StudentIds.Distinct())
        {
            students[studentId] = await users.GetUserAsync(studentId, cancellationToken);
            if (await creditHours.IsOnCreditHoursAsync(studentId, cancellationToken))
                onCreditHours.Add(studentId);
        }

        var existing = await db.CourseRegistrations.AsNoTracking()
            .Where(r => command.StudentIds.Contains(r.StudentId) &&
                        r.TermId == term.Id &&
                        r.SubjectId == subject.Id &&
                        r.Status != RegistrationStatus.Dropped)
            .Select(r => new { r.StudentId, r.Id })
            .ToListAsync(cancellationToken);

        var registrationByStudent = existing
            .GroupBy(r => r.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Id).First().Id);

        var results = new List<MilitaryEducationAssignmentResultDto>(command.StudentIds.Count);
        var now = clock.GetUtcNow();

        await seats.BeginAsync(cancellationToken);
        try
        {
            foreach (var studentId in command.StudentIds)
            {
                var student = students.GetValueOrDefault(studentId);
                if (student is null || student.Role != PortalRoles.Student || !student.IsActive)
                {
                    results.Add(new MilitaryEducationAssignmentResultDto(studentId, "not_a_student", null));
                    continue;
                }

                // Decision LIFECYCLEDEPT: military education is a credit-hours registration; year-group students are not assigned here.
                if (!onCreditHours.Contains(studentId))
                {
                    results.Add(new MilitaryEducationAssignmentResultDto(studentId, "other_academic_system", null));
                    continue;
                }

                if (registrationByStudent.TryGetValue(studentId, out var existingRegistrationId))
                {
                    results.Add(new MilitaryEducationAssignmentResultDto(
                        studentId, "already_registered", existingRegistrationId));
                    continue;
                }

                if (!await seats.TryTakeAsync(group.Id, group.Capacity, cancellationToken))
                    throw new ConflictException(FullMessage);

                var registration = CourseRegistration.CreatePendingPayment(
                    studentId, term.Id, subject.Id, group.Id, subject.CreditHours, now);

                db.CourseRegistrations.Add(registration);
                events.Add(new MilitaryEducationRequested(
                    Guid.CreateVersion7(),
                    now,
                    studentId,
                    registration.Id,
                    subject.Id,
                    term.Id));

                registrationByStudent[studentId] = registration.Id;
                results.Add(new MilitaryEducationAssignmentResultDto(
                    studentId, "assigned", registration.Id));
            }
        }
        catch
        {
            await seats.RollbackAsync(cancellationToken);
            throw;
        }

        return new MilitaryEducationAssignmentDto(results);
    }
}
