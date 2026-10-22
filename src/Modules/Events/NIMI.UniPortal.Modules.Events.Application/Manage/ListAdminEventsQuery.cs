using NIMI.UniPortal.Modules.Events.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

/// <summary>Every event (drafts and cancelled included) for Student Affairs, by start time.</summary>
public sealed record ListAdminEventsQuery(string? Status, DateTimeOffset? From, DateTimeOffset? To, Guid AdminId, PageRequest Paging)
    : IQuery<PagedResult<EventDto>>;
