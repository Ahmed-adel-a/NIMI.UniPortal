namespace NIMI.UniPortal.Modules.Announcements.Domain;

/// <summary>How loud an announcement is; only <see cref="Urgent"/> notifies the students (R5).</summary>
public enum AnnouncementPriority
{
    Normal,
    High,
    Urgent,
}
