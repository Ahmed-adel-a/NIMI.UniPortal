using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.LiveRoster;

/// <summary>P6.4: the professor's live view of who has checked in so far (polled, no websockets).</summary>
public sealed record GetLiveRosterQuery(Guid SessionId, Guid CallerId) : IQuery<LiveRosterDto>;
