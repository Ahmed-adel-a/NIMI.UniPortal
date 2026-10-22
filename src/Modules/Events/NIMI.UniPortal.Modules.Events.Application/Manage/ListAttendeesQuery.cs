using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Events.Application.Manage;

/// <summary>An event's registrants (registered or attended), oldest registration first.</summary>
public sealed record ListAttendeesQuery(Guid EventId, PageRequest Paging) : IQuery<PagedResult<AttendeeDto>>;
