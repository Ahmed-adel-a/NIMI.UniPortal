using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Emails;

/// <summary>The outbox, newest first, optionally for one address (development and test environments only).</summary>
public sealed record ListOutboxQuery(string? To, PageRequest Paging) : IQuery<PagedResult<OutboundEmailDto>>;
