using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

internal sealed class PublishEventHandler(IEventsDbContext db, EventPresenter presenter, TimeProvider clock)
    : IRequestHandler<PublishEventCommand, EventDto>
{
    public async Task<EventDto> Handle(PublishEventCommand command, CancellationToken cancellationToken)
    {
        var campusEvent = await db.Events.SingleOrDefaultAsync(e => e.Id == command.EventId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");
        if (campusEvent.Status != EventStatus.Draft)
            throw new ConflictException("Only a draft event can be published.");

        campusEvent.Publish(clock.GetUtcNow());
        return await presenter.PresentAsync(campusEvent, command.AdminId, cancellationToken);
    }
}
