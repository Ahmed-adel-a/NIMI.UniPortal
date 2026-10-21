using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Devices.RequestChange;

/// <summary>P6.8: a student asks Student Affairs to move attendance to a new phone (DEVICE).</summary>
public sealed record RequestDeviceChangeCommand(
    Guid StudentId,
    bool IsStudent,
    string NewFingerprint,
    string Reason) : ICommand<DeviceChangeDto>;
