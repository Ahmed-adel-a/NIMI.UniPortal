namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.LiveRoster;

/// <summary>Every confirmed member with their current status (<c>not_yet</c> when there is no record) and the totals.</summary>
public sealed record LiveRosterDto(
    Guid SessionId,
    bool IsOpen,
    IReadOnlyList<LiveRosterItemDto> Items,
    LiveRosterCountsDto Counts);
