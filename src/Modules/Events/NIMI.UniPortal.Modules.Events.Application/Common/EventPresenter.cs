using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Domain;

namespace NIMI.UniPortal.Modules.Events.Application.Common;

/// <summary>Adds seats taken (registered + attended) and the caller's own status to events, counted in SQL.</summary>
internal sealed class EventPresenter(IEventsDbContext db)
{
    public async Task<EventDto> PresentAsync(CampusEvent campusEvent, Guid viewerId, CancellationToken cancellationToken) =>
        (await PresentAllAsync([campusEvent], viewerId, cancellationToken))[0];

    public async Task<IReadOnlyList<EventDto>> PresentAllAsync(
        IReadOnlyList<CampusEvent> events, Guid viewerId, CancellationToken cancellationToken)
    {
        var ids = events.Select(e => e.Id).ToList();
        var taken = await db.Registrations.AsNoTracking()
            .Where(r => ids.Contains(r.EventId) &&
                        (r.Status == EventRegistrationStatus.Registered || r.Status == EventRegistrationStatus.Attended))
            .GroupBy(r => r.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count, cancellationToken);
        var mine = await db.Registrations.AsNoTracking()
            .Where(r => ids.Contains(r.EventId) && r.UserId == viewerId)
            .ToDictionaryAsync(r => r.EventId, r => r.Status, cancellationToken);

        return [.. events.Select(e => EventDto.From(
            e, taken.GetValueOrDefault(e.Id), mine.TryGetValue(e.Id, out var status) ? status : null))];
    }
}
