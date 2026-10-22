using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Notifications.Application.Abstractions;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Notifications.Application.Emails;

internal sealed class ListOutboxHandler(INotificationsDbContext db) : IRequestHandler<ListOutboxQuery, PagedResult<OutboundEmailDto>>
{
    public async Task<PagedResult<OutboundEmailDto>> Handle(ListOutboxQuery query, CancellationToken cancellationToken)
    {
        var emails = db.OutboundEmails.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.To))
        {
            var to = query.To.Trim().ToLowerInvariant();
            emails = emails.Where(e => e.To.ToLower() == to);
        }

        var total = await emails.CountAsync(cancellationToken);
        var page = await emails
            .OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id)
            .Skip(query.Paging.Skip)
            .Take(query.Paging.PageSize)
            .Select(e => new OutboundEmailDto(e.Id, e.To, e.Subject, e.Body, e.Provider, e.CreatedAt, e.SentAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<OutboundEmailDto>(page, query.Paging.Page, query.Paging.PageSize, total);
    }
}
