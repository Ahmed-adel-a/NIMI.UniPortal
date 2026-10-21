using NIMI.UniPortal.EventBus;

namespace NIMI.UniPortal.Modules.Notifications.Contracts;

/// <summary>
/// Decision NOTIFYEVENT: a producer raises a notification by queuing this after its own commit; the
/// Notifications module stores one row per distinct recipient and does not reference the producer.
/// <see cref="Type"/> is one of the stored CHECK values (schedule, assignment, grade, attendance,
/// announcement, payment, forum, event, system); texts are composed by the producer, in English and Arabic.
/// </summary>
public sealed record NotificationRequested(
    Guid EventId,
    DateTimeOffset OccurredAt,
    IReadOnlyList<Guid> RecipientUserIds,
    string Type,
    string Title,
    string TitleAr,
    string Message,
    string MessageAr,
    string? ActionUrl) : IIntegrationEvent;
