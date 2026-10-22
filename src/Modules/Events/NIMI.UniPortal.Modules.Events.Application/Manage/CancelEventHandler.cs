using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

internal sealed class CancelEventHandler(IEventsDbContext db, EventPresenter presenter, RegistrantNotices notices, TimeProvider clock)
    : IRequestHandler<CancelEventCommand, EventDto>
{
    public async Task<EventDto> Handle(CancelEventCommand command, CancellationToken cancellationToken)
    {
        var campusEvent = await db.Events.SingleOrDefaultAsync(e => e.Id == command.EventId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");
        if (campusEvent.Status == EventStatus.Cancelled)
            throw new ConflictException("This event is already cancelled.");

        var now = clock.GetUtcNow();
        campusEvent.Cancel(command.Reason ?? string.Empty, now);
        await notices.CancelledAsync(campusEvent, now, cancellationToken);
        return await presenter.PresentAsync(campusEvent, command.AdminId, cancellationToken);
    }
}
