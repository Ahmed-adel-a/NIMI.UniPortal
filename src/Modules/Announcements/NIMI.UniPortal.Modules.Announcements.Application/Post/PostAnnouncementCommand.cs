using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.Modules.Announcements.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Announcements.Application.Post;

/// <summary>P7.4: a teaching professor posts to a subject's students — all groups or one (ANN).</summary>
public sealed record PostAnnouncementCommand(
    Guid SubjectId,
    Guid? SubjectGroupId,
    string Title,
    string Body,
    AnnouncementPriority Priority,
    bool IsPinned,
    DateTimeOffset? ExpiresAt,
    Viewer Caller) : ICommand<AnnouncementDto>;
