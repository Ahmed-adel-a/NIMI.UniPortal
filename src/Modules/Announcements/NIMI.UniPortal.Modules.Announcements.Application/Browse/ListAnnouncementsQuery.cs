using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Announcements.Application.Browse;

/// <summary>P7.5: one subject's announcements this term — pinned first, then newest.</summary>
public sealed record ListAnnouncementsQuery(Guid? SubjectId, Viewer Caller, PageRequest Paging)
    : IQuery<PagedResult<AnnouncementDto>>;
