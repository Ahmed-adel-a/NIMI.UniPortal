using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.Modules.Announcements.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Announcements.Application.Manage;

/// <summary>P7.4: the author or a teacher of the subject rewrites the announcement (the subject and group stay).</summary>
public sealed record EditAnnouncementCommand(
    Guid AnnouncementId,
    string Title,
    string Body,
    AnnouncementPriority Priority,
    bool IsPinned,
    DateTimeOffset? ExpiresAt,
    Viewer Caller) : ICommand<AnnouncementDto>;
