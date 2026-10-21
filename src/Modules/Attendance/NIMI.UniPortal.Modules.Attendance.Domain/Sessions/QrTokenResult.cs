namespace NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

/// <summary>The outcome of verifying a rotating QR token (phase §5, P6.1).</summary>
public enum QrTokenResult
{
    Valid,
    Malformed,
    WrongSession,
    BadSignature,
    Expired,
}
