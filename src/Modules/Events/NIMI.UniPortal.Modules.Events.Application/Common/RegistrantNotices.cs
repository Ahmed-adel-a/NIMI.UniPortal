using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.Modules.Notifications.Contracts;
using NIMI.UniPortal.Modules.Platform.Contracts;

namespace NIMI.UniPortal.Modules.Events.Application.Common;

/// <summary>
/// Rule R4 — tells everyone registered (not cancelled) about a new time or venue, or a cancellation, in English and
/// Arabic, with wall-clock times in the event faculty's time zone. Queued and stored after the commit.
/// </summary>
internal sealed class RegistrantNotices(IEventsDbContext db, IPlatformSettingsApi platform, IEventCollector events)
{
    public async Task ChangedAsync(CampusEvent campusEvent, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var when = await WhenAsync(campusEvent, cancellationToken);
        await NotifyAsync(
            campusEvent,
            now,
            $"Event updated: {campusEvent.Title}",
            $"تحديث فعالية: {campusEvent.Title}",
            $"Now {when} at {campusEvent.Venue}.",
            $"الموعد الجديد {when} في {campusEvent.Venue}.",
            cancellationToken);
    }

    public Task CancelledAsync(CampusEvent campusEvent, DateTimeOffset now, CancellationToken cancellationToken) =>
        NotifyAsync(
            campusEvent,
            now,
            $"Event cancelled: {campusEvent.Title}",
            $"إلغاء فعالية: {campusEvent.Title}",
            $"Reason: {campusEvent.CancellationReason}",
            $"السبب: {campusEvent.CancellationReason}",
            cancellationToken);

    private async Task NotifyAsync(
        CampusEvent campusEvent, DateTimeOffset now, string title, string titleAr, string message, string messageAr, CancellationToken cancellationToken)
    {
        var recipients = await db.Registrations.AsNoTracking()
            .Where(r => r.EventId == campusEvent.Id && r.Status == EventRegistrationStatus.Registered)
            .Select(r => r.UserId)
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
            return;

        events.Add(new NotificationRequested(
            Guid.CreateVersion7(), now, recipients, "event", Clip(title, 200), Clip(titleAr, 200), Clip(message, 2000), Clip(messageAr, 2000),
            $"/events/{campusEvent.Id}"));
    }

    private async Task<string> WhenAsync(CampusEvent campusEvent, CancellationToken cancellationToken)
    {
        var settings = await platform.GetEffectiveSettingsAsync(campusEvent.FacultyId, cancellationToken);
        var local = TimeZoneInfo.ConvertTime(campusEvent.StartsAt, TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone));
        return local.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private static string Clip(string text, int max) => text.Length <= max ? text : text[..(max - 3)] + "...";
}
