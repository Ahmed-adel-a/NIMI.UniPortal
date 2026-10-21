using NIMI.UniPortal.Modules.Announcements.Domain;

namespace NIMI.UniPortal.Modules.Announcements.Application;

/// <summary>An announcement as the API shows it (phase §6b). <c>priority</c> serializes as snake_case text.</summary>
public sealed record AnnouncementDto(
    Guid Id,
    Guid SubjectId,
    string SubjectCode,
    Guid? SubjectGroupId,
    string Title,
    string Body,
    AnnouncementPriority Priority,
    bool IsPinned,
    DateTimeOffset? ExpiresAt,
    Guid AuthorId,
    string AuthorName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static AnnouncementDto From(Announcement announcement, string subjectCode, string authorName) =>
        new(announcement.Id,
            announcement.SubjectId,
            subjectCode,
            announcement.SubjectGroupId,
            announcement.Title,
            announcement.Body,
            announcement.Priority,
            announcement.IsPinned,
            announcement.ExpiresAt,
            announcement.AuthorId,
            authorName,
            announcement.CreatedAt,
            announcement.UpdatedAt);
}
