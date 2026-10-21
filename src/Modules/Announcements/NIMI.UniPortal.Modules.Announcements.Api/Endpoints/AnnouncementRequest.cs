using NIMI.UniPortal.Modules.Announcements.Domain;

namespace NIMI.UniPortal.Modules.Announcements.Api.Endpoints;

/// <summary>POST /api/announcements (subject and group are ignored on PUT).</summary>
public sealed record AnnouncementRequest(
    Guid SubjectId,
    Guid? SubjectGroupId,
    string? Title,
    string? Body,
    AnnouncementPriority? Priority,
    bool IsPinned,
    DateTimeOffset? ExpiresAt);
