namespace NIMI.UniPortal.Modules.Announcements.Contracts;

/// <summary>What other modules (and the host's dashboards, P10) may read from Announcements.</summary>
public interface IAnnouncementsApi
{
    /// <summary>The top of the student's feed (pinned first, then newest; expired ones never appear).</summary>
    Task<IReadOnlyList<AnnouncementRef>> GetStudentFeedAsync(Guid studentId, int count, CancellationToken cancellationToken = default);
}
