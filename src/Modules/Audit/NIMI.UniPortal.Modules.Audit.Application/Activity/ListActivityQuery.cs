using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Audit.Application.Activity;

/// <summary>P10.2: the activity log, newest first; every filter is optional, <c>from</c>/<c>to</c> are inclusive.</summary>
public sealed record ListActivityQuery(
    Guid? ActorId,
    string? EntityType,
    Guid? EntityId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    PageRequest Paging) : IQuery<PagedResult<ActivityItemDto>>;
