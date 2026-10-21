using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.CloseSession;

/// <summary>P6.5: ends the session; every confirmed member without a record becomes absent (R8). Idempotent.</summary>
public sealed record CloseSessionCommand(Guid SessionId, Guid CallerId) : ICommand<CloseSummaryDto>;
