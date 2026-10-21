using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Announcements.Application.Browse;

/// <summary>P7.5: a student's announcements across every subject they are registered in this term.</summary>
public sealed record GetFeedQuery(Viewer Caller, PageRequest Paging) : IQuery<PagedResult<AnnouncementDto>>;
