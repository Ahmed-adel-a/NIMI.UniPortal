using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.CheckIn;

/// <summary>P6.2: a student scans the professor's current QR from their registered phone.</summary>
public sealed record CheckInCommand(
    string Token,
    string DeviceFingerprint,
    Guid StudentId,
    bool IsStudent) : ICommand<CheckInResultDto>;
