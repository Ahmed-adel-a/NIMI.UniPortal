using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Devices.Decide;

/// <summary>P6.8: Student Affairs approves (old phone revoked, new one registered) or rejects a pending request.</summary>
public sealed record DecideDeviceChangeCommand(Guid RequestId, bool Approve, Guid DecidedBy) : ICommand<DeviceChangeDto>;
