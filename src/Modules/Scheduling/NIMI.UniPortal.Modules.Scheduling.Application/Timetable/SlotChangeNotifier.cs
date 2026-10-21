using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Notifications.Contracts;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Timetable;

/// <summary>
/// P10.3 — tells the people a slot change affects: the confirmed students of the slot's group (old and new group when
/// it moved between groups) and its staff (old and new lecturer). Texts give old → new day, time and hall in English
/// and Arabic; the notification is queued and stored after the commit.
/// </summary>
internal sealed class SlotChangeNotifier(IAcademicLifecycle lifecycle, IEventCollector events)
{
    private const string ActionUrl = "/schedule";

    public async Task MovedAsync(
        SlotSnapshot before, SlotSnapshot after, string subjectCode, int groupNumber, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (before == after)
            return;

        var recipients = await RecipientsAsync([before.GroupId, after.GroupId], [before.StaffId, after.StaffId], cancellationToken);
        events.Add(new NotificationRequested(
            Guid.CreateVersion7(),
            now,
            recipients,
            "schedule",
            $"Timetable change: {subjectCode}",
            $"تغيير في الجدول: {subjectCode}",
            $"{subjectCode} group {groupNumber}: {En(before)} → {En(after)}.",
            $"{subjectCode} مجموعة {groupNumber}: {Ar(before)} ← {Ar(after)}.",
            ActionUrl));
    }

    public async Task CancelledAsync(
        SlotSnapshot slot, string subjectCode, int groupNumber, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var recipients = await RecipientsAsync([slot.GroupId], [slot.StaffId], cancellationToken);
        events.Add(new NotificationRequested(
            Guid.CreateVersion7(),
            now,
            recipients,
            "schedule",
            $"Lecture cancelled: {subjectCode}",
            $"إلغاء محاضرة: {subjectCode}",
            $"{subjectCode} group {groupNumber} on {En(slot)} is cancelled.",
            $"تم إلغاء {subjectCode} مجموعة {groupNumber} يوم {Ar(slot)}.",
            ActionUrl));
    }

    private async Task<IReadOnlyList<Guid>> RecipientsAsync(
        IEnumerable<Guid> groupIds, IEnumerable<Guid?> staffIds, CancellationToken cancellationToken)
    {
        var recipients = new HashSet<Guid>();
        foreach (var groupId in groupIds.Distinct())
            recipients.UnionWith(await lifecycle.GetStudentsInGroupAsync(groupId, cancellationToken));
        foreach (var staffId in staffIds)
        {
            if (staffId is { } id)
                recipients.Add(id);
        }

        return [.. recipients];
    }

    private static string En(SlotSnapshot s) => $"{ProductDays.En(s.DayOfWeek)} {s.StartTime:HH:mm}–{s.EndTime:HH:mm}, {s.FacilityName}";

    private static string Ar(SlotSnapshot s) => $"{ProductDays.Ar(s.DayOfWeek)} {s.StartTime:HH:mm}–{s.EndTime:HH:mm}، {s.FacilityName}";
}
