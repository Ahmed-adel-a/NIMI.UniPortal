using NIMI.UniPortal.Modules.Announcements.Application.Browse;
using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.Modules.Announcements.Contracts;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Announcements.Application;

/// <summary>The Announcements contract, answered by the student feed query itself (one rule, one place).</summary>
internal sealed class AnnouncementsApi(ISender sender) : IAnnouncementsApi
{
    public async Task<IReadOnlyList<AnnouncementRef>> GetStudentFeedAsync(
        Guid studentId, int count, CancellationToken cancellationToken = default)
    {
        var feed = await sender.Send(
            new GetFeedQuery(new Viewer(studentId, false, false, true), new PageRequest(1, count)), cancellationToken);
        return [.. feed.Items.Select(a => new AnnouncementRef(
            a.Id, a.SubjectId, a.SubjectCode, a.Title, a.Priority.ToString().ToLowerInvariant(), a.IsPinned, a.CreatedAt))];
    }
}
