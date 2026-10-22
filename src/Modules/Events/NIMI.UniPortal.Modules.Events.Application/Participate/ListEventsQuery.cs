using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

/// <summary>Published events of the caller's audience that haven't ended, soonest first.</summary>
public sealed record ListEventsQuery(Viewer Caller, DateTimeOffset? From, DateTimeOffset? To, PageRequest Paging)
    : IQuery<PagedResult<EventDto>>;
