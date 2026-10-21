using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.UpdateScheduleSlot;

internal sealed class UpdateScheduleSlotHandler(
    ISchedulingDbContext db,
    IUsersApi users,
    IPlatformSettingsApi platform,
    IEventCollector events,
    ICurrentUser currentUser,
    SlotChangeNotifier notifier,
    TimeProvider clock) : IRequestHandler<UpdateScheduleSlotCommand, ScheduleSlotDto>
{
    public async Task<ScheduleSlotDto> Handle(
        UpdateScheduleSlotCommand command, CancellationToken cancellationToken)
    {
        var slot = await db.ScheduleSlots
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No timetable slot with id '{command.Id}'.");

        var oldFacilityName = await db.Facilities
            .Where(f => f.Id == slot.FacilityId)
            .Select(f => f.Name)
            .SingleAsync(cancellationToken);
        var before = SlotSnapshot.Of(slot, oldFacilityName);

        var group = await db.SubjectGroups
            .Include(g => g.Subject)
            .FirstOrDefaultAsync(g => g.Id == command.SubjectGroupId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.SubjectGroupId), "Unknown group.");

        var facility = await db.Facilities
            .FirstOrDefaultAsync(f => f.Id == command.FacilityId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.FacilityId), "Unknown facility.");

        var term = await db.Terms
            .FirstOrDefaultAsync(t => t.Id == group.TermId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.SubjectGroupId), "The group's term is missing.");

        if (command.StaffId is { } staffId)
        {
            var staff = await users.GetUserAsync(staffId, cancellationToken);
            if (staff is null || !staff.IsActive || staff.Role != PortalRoles.Professor)
                throw ValidationErrors.For(nameof(command.StaffId), "Must be an active professor.");
        }

        var department = await users.GetDepartmentAsync(group.Subject.DepartmentId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.SubjectGroupId), "The subject's department is missing.");

        var settings = await platform.GetEffectiveSettingsAsync(department.FacultyId, cancellationToken);
        if (!settings.WorkingDays.Contains(command.DayOfWeek))
            throw ValidationErrors.For(nameof(command.DayOfWeek), "That day is not a working day for this faculty.");

        slot.Update(
            group,
            facility,
            command.StaffId,
            command.DayOfWeek,
            command.StartTime,
            command.EndTime,
            command.Recurrence,
            command.ValidFrom,
            command.ValidUntil,
            term,
            clock.GetUtcNow());

        var conflict = await ScheduleSlotConflicts.FindAsync(db, slot, slot.Id, cancellationToken);
        if (conflict is not null)
            throw new ConflictException(ScheduleSlotConflicts.Describe(slot, conflict));

        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            currentUser.UserId,
            AuditActions.ScheduleSlotUpdated,
            AuditEntities.ScheduleSlot,
            slot.Id,
            $"Slot of {group.Subject.Code} group {group.GroupNumber} set to day {slot.DayOfWeek}, {slot.StartTime:HH:mm}–{slot.EndTime:HH:mm}, {facility.Name}."));
        await notifier.MovedAsync(
            before, SlotSnapshot.Of(slot, facility.Name), group.Subject.Code, group.GroupNumber, clock.GetUtcNow(), cancellationToken);

        return ScheduleSlotDto.From(slot, group, facility);
    }
}
