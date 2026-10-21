using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Abstractions;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.ApplyForMilitaryEducation;

/// <summary>
/// P5.5 — a student applies for military education in opt-in mode. The group must be an active military-education
/// group of the active term; the effective mode belongs to the student's faculty (D5/MILEDU). A seat is reserved
/// exactly like a normal registration and the row starts at <c>pending_payment</c>. The fee is requested through
/// <see cref="MilitaryEducationRequested"/> only after the commit, so Payments can create it idempotently.
/// </summary>
internal sealed class ApplyForMilitaryEducationHandler(
    ICreditHoursDbContext db,
    ISeatCounter seats,
    ISchedulingApi scheduling,
    IUsersApi users,
    IPlatformSettingsApi platform,
    IEventCollector events,
    TimeProvider clock)
    : IRequestHandler<ApplyForMilitaryEducationCommand, MilitaryEducationApplicationDto>
{
    private const string Field = "subjectGroupId";
    private const string MilitaryEducation = "military_education";
    private const string OptIn = "opt_in";
    private const string Mandatory = "mandatory";
    private const string FullMessage = "The group is full.";

    public async Task<MilitaryEducationApplicationDto> Handle(
        ApplyForMilitaryEducationCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsStudent)
            throw new ForbiddenException("Only students can apply for military education.");

        var student = await users.GetUserAsync(command.StudentId, cancellationToken)
            ?? throw new NotFoundException("The signed-in student was not found.");

        var placement = await users.GetStudentPlacementAsync(student.Id, cancellationToken);
        if (placement is null || !placement.IsActive)
            throw new ForbiddenException("Only active students can apply for military education.");

        var term = await scheduling.GetCurrentTermAsync(cancellationToken)
            ?? throw ValidationErrors.For(Field, "There is no active term.");

        var group = await scheduling.GetGroupAsync(command.SubjectGroupId, cancellationToken);
        if (group is null || !group.IsActive || group.TermId != term.Id)
            throw ValidationErrors.For(Field, "This group is not offered in the active term.");

        var subject = await scheduling.GetSubjectAsync(group.SubjectId, cancellationToken);
        if (subject is null || subject.Category != MilitaryEducation)
            throw ValidationErrors.For(Field, "This group is not a military-education group.");

        var settings = await platform.GetEffectiveSettingsAsync(placement.FacultyId, cancellationToken);
        if (settings.MilitaryEducationMode == Mandatory)
            throw ValidationErrors.For(Field, "Military education is assigned by Student Affairs in this faculty.");

        if (settings.MilitaryEducationMode != OptIn)
            throw ValidationErrors.For(Field, "Military education is not available in this faculty.");

        // R4 in the military flow: one live registration per subject and term (the database index is the backstop).
        var alreadyApplied = await db.CourseRegistrations.AsNoTracking()
            .AnyAsync(
                r => r.StudentId == student.Id &&
                     r.TermId == term.Id &&
                     r.SubjectId == subject.Id &&
                     r.Status != RegistrationStatus.Dropped,
                cancellationToken);

        if (alreadyApplied)
            throw new ConflictException("Already applied for military education.");

        await seats.BeginAsync(cancellationToken);
        try
        {
            if (!await seats.TryTakeAsync(group.Id, group.Capacity, cancellationToken))
                throw new ConflictException(FullMessage);

            var now = clock.GetUtcNow();
            var registration = CourseRegistration.CreatePendingPayment(
                student.Id, term.Id, subject.Id, group.Id, subject.CreditHours, now);

            db.CourseRegistrations.Add(registration);
            events.Add(new MilitaryEducationRequested(
                Guid.CreateVersion7(),
                now,
                student.Id,
                registration.Id,
                subject.Id,
                term.Id));

            return new MilitaryEducationApplicationDto(
                registration.Id,
                subject.Id,
                subject.Code,
                group.Id,
                group.GroupNumber,
                "pending_payment");
        }
        catch
        {
            await seats.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
