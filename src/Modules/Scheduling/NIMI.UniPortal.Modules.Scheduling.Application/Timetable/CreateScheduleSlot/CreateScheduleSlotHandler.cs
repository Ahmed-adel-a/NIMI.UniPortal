using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Platform.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.Modules.Scheduling.Domain.Timetable;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.CreateScheduleSlot;

internal sealed class CreateScheduleSlotHandler(
    ISchedulingDbContext db,
    IUsersApi users,
    IPlatformSettingsApi platform,
    TimeProvider clock) : IRequestHandler<CreateScheduleSlotCommand, ScheduleSlotDto>
{
    public async Task<ScheduleSlotDto> Handle(
        CreateScheduleSlotCommand command, CancellationToken cancellationToken)
    {
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
            _ = await users.GetUserAsync(staffId, cancellationToken)
                ?? throw ValidationErrors.For(nameof(command.StaffId), "Unknown staff member.");

        var department = await users.GetDepartmentAsync(group.Subject.DepartmentId, cancellationToken)
            ?? throw ValidationErrors.For(nameof(command.SubjectGroupId), "The subject's department is missing.");

        var settings = await platform.GetEffectiveSettingsAsync(department.FacultyId, cancellationToken);
        if (!settings.WorkingDays.Contains(command.DayOfWeek))
            throw ValidationErrors.For(nameof(command.DayOfWeek), "That day is not a working day for this faculty.");

        var slot = ScheduleSlot.Create(
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
            command.CreatedBy,
            clock.GetUtcNow());

        var conflict = await ScheduleSlotConflicts.FindAsync(db, slot, excludeId: null, cancellationToken);
        if (conflict is not null)
            throw new ConflictException(ScheduleSlotConflicts.Describe(slot, conflict));

        db.ScheduleSlots.Add(slot);
        return ScheduleSlotDto.From(slot, group, facility);
    }
}
