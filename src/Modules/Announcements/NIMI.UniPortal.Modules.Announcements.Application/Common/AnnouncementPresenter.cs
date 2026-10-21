using NIMI.UniPortal.Modules.Announcements.Domain;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Announcements.Application.Common;

/// <summary>Turns announcements into DTOs, looking each subject code and author name up once per call.</summary>
internal sealed class AnnouncementPresenter(ISchedulingApi scheduling, IUsersApi users)
{
    private readonly Dictionary<Guid, string> _codes = [];
    private readonly Dictionary<Guid, string> _authors = [];

    public async Task<AnnouncementDto> PresentAsync(Announcement announcement, CancellationToken cancellationToken)
    {
        if (!_codes.TryGetValue(announcement.SubjectId, out var code))
        {
            code = (await scheduling.GetSubjectAsync(announcement.SubjectId, cancellationToken))?.Code ?? string.Empty;
            _codes[announcement.SubjectId] = code;
        }

        if (!_authors.TryGetValue(announcement.AuthorId, out var author))
        {
            author = (await users.GetUserAsync(announcement.AuthorId, cancellationToken))?.FullName ?? string.Empty;
            _authors[announcement.AuthorId] = author;
        }

        return AnnouncementDto.From(announcement, code, author);
    }

    public async Task<IReadOnlyList<AnnouncementDto>> PresentAllAsync(IEnumerable<Announcement> announcements, CancellationToken cancellationToken)
    {
        var dtos = new List<AnnouncementDto>();
        foreach (var announcement in announcements)
            dtos.Add(await PresentAsync(announcement, cancellationToken));
        return dtos;
    }
}
