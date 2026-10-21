using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.CancelScheduleSlot;

internal sealed class CancelScheduleSlotHandler(
    ISchedulingDbContext db, IEventCollector events, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<CancelScheduleSlotCommand, Unit>
{
    public async Task<Unit> Handle(CancelScheduleSlotCommand command, CancellationToken cancellationToken)
    {
        var slot = await db.ScheduleSlots
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No timetable slot with id '{command.Id}'.");

        slot.Cancel(clock.GetUtcNow());
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            currentUser.UserId,
            AuditActions.ScheduleSlotCancelled,
            AuditEntities.ScheduleSlot,
            slot.Id,
            $"Slot on day {slot.DayOfWeek}, {slot.StartTime:HH:mm}–{slot.EndTime:HH:mm} cancelled."));
        return Unit.Value;
    }
}
