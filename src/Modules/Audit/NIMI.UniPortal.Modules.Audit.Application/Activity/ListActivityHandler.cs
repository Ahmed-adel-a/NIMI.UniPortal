using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Audit.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Audit.Application.Activity;

internal sealed class ListActivityHandler(IAuditDbContext db, IUsersApi users)
    : IRequestHandler<ListActivityQuery, PagedResult<ActivityItemDto>>
{
    public async Task<PagedResult<ActivityItemDto>> Handle(ListActivityQuery query, CancellationToken cancellationToken)
    {
        var logs = db.ActivityLogs.AsNoTracking();
        if (query.ActorId is { } actorId)
            logs = logs.Where(l => l.ActorId == actorId);
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            logs = logs.Where(l => l.EntityType == query.EntityType);
        if (query.EntityId is { } entityId)
            logs = logs.Where(l => l.EntityId == entityId);
        if (query.From is { } from)
            logs = logs.Where(l => l.OccurredAt >= from);
        if (query.To is { } to)
            logs = logs.Where(l => l.OccurredAt <= to);

        var total = await logs.CountAsync(cancellationToken);
        var page = await logs
            .OrderByDescending(l => l.OccurredAt)
            .ThenByDescending(l => l.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .ToListAsync(cancellationToken);

        var names = new Dictionary<Guid, string?>();
        foreach (var actor in page.Where(l => l.ActorId is not null).Select(l => l.ActorId!.Value).Distinct())
            names[actor] = (await users.GetUserAsync(actor, cancellationToken))?.FullName;

        return new PagedResult<ActivityItemDto>(
            [.. page.Select(l => new ActivityItemDto(
                l.Id,
                l.ActorId,
                l.ActorId is { } id ? names.GetValueOrDefault(id) : null,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.Summary,
                l.OccurredAt))],
            query.Paging.Page,
            query.Paging.PageSize,
            total);
    }
}
