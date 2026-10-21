using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Announcements.Application.Manage;

/// <summary>P7.4: soft delete by the author or a teacher of the subject.</summary>
public sealed record DeleteAnnouncementCommand(Guid AnnouncementId, Viewer Caller) : ICommand<Unit>;
