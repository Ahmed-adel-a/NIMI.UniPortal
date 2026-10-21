namespace NIMI.UniPortal.Modules.Notifications.Domain;

/// <summary>The kinds of notification a producer may request (stored as lower-case text, CHECK-constrained).</summary>
public enum NotificationType
{
    Schedule,
    Assignment,
    Grade,
    Attendance,
    Announcement,
    Payment,
    Forum,
    Event,
    System,
}
