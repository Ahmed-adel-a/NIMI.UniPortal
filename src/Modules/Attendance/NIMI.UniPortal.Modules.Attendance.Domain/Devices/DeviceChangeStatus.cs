namespace NIMI.UniPortal.Modules.Attendance.Domain.Devices;

/// <summary>Decision DEVICE: a phone change waits for Student Affairs.</summary>
public enum DeviceChangeStatus
{
    Pending,
    Approved,
    Rejected,
}
