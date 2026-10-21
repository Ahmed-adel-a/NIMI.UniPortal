using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.GetSessionQr;

/// <summary>P6.1: a fresh rotating token for the session's screen.</summary>
public sealed record GetSessionQrQuery(Guid SessionId, Guid CallerId) : IQuery<SessionQrDto>;
