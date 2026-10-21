namespace NIMI.UniPortal.Modules.Announcements.Contracts;

/// <summary>An announcement as other modules see it; <see cref="Priority"/> is snake_case text (normal, high, urgent).</summary>
public sealed record AnnouncementRef(
    Guid Id,
    Guid SubjectId,
    string SubjectCode,
    string Title,
    string Priority,
    bool IsPinned,
    DateTimeOffset CreatedAt);
