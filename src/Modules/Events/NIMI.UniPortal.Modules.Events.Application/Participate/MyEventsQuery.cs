using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Events.Application.Participate;

/// <summary>Every event the caller ever registered for, with their status, by start time.</summary>
public sealed record MyEventsQuery(Viewer Caller, PageRequest Paging) : IQuery<PagedResult<EventDto>>;
