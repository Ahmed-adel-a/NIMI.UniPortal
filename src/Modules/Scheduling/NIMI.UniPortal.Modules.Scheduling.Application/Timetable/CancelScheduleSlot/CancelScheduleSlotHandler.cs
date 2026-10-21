using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Audit.Contracts;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.CancelScheduleSlot;

internal sealed class CancelScheduleSlotHandler(
    ISchedulingDbContext db, IEventCollector events, ICurrentUser currentUser, SlotChangeNotifier notifier, TimeProvider clock)
    : IRequestHandler<CancelScheduleSlotCommand, Unit>
{
    public async Task<Unit> Handle(CancelScheduleSlotCommand command, CancellationToken cancellationToken)
    {
        var slot = await db.ScheduleSlots
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No timetable slot with id '{command.Id}'.");

        var wasCancelled = slot.IsCancelled;
        slot.Cancel(clock.GetUtcNow());
        events.Add(new AuditableActionPerformed(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            currentUser.UserId,
            AuditActions.ScheduleSlotCancelled,
            AuditEntities.ScheduleSlot,
            slot.Id,
            $"Slot on day {slot.DayOfWeek}, {slot.StartTime:HH:mm}–{slot.EndTime:HH:mm} cancelled."));

        if (!wasCancelled)
        {
            var group = await db.SubjectGroups.AsNoTracking()
                .Include(g => g.Subject)
                .SingleAsync(g => g.Id == slot.SubjectGroupId, cancellationToken);
            var facilityName = await db.Facilities.AsNoTracking()
                .Where(f => f.Id == slot.FacilityId)
                .Select(f => f.Name)
                .SingleAsync(cancellationToken);
            await notifier.CancelledAsync(
                SlotSnapshot.Of(slot, facilityName), group.Subject.Code, group.GroupNumber, clock.GetUtcNow(), cancellationToken);
        }

        return Unit.Value;
    }
}
