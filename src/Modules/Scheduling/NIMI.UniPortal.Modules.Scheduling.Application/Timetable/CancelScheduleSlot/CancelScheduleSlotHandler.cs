using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Scheduling.Application.Abstractions;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable.CancelScheduleSlot;

internal sealed class CancelScheduleSlotHandler(ISchedulingDbContext db, TimeProvider clock)
    : IRequestHandler<CancelScheduleSlotCommand, Unit>
{
    public async Task<Unit> Handle(CancelScheduleSlotCommand command, CancellationToken cancellationToken)
    {
        var slot = await db.ScheduleSlots
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"No timetable slot with id '{command.Id}'.");

        slot.Cancel(clock.GetUtcNow());
        return Unit.Value;
    }
}
