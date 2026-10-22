using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

internal sealed class GetEventHandler(EventAudience audience, EventPresenter presenter) : IRequestHandler<GetEventQuery, EventDto>
{
    public async Task<EventDto> Handle(GetEventQuery query, CancellationToken cancellationToken) =>
        await presenter.PresentAsync(
            await audience.VisibleAsync(query.EventId, query.Caller, cancellationToken), query.Caller.UserId, cancellationToken);
}
