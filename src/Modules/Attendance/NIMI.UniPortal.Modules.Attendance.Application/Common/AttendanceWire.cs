using NIMI.UniPortal.Modules.Attendance.Domain.Devices;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;

namespace NIMI.UniPortal.Modules.Attendance.Application.Common;

/// <summary>The single place that maps attendance enums to the snake_case strings the HTTP API speaks.</summary>
internal static class AttendanceWire
{
    public static string Status(AttendanceStatus status) =>
        status switch
        {
            AttendanceStatus.Present => "present",
            AttendanceStatus.Late => "late",
            AttendanceStatus.ManualAudit => "manual_audit",
            AttendanceStatus.Absent => "absent",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown attendance status."),
        };

    public static string Method(AttendanceMethod method) =>
        method switch
        {
            AttendanceMethod.QrScan => "qr_scan",
            AttendanceMethod.ManualEntry => "manual_entry",
            AttendanceMethod.AutoAbsent => "auto_absent",
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown attendance method."),
        };

    public static string DeviceStatus(DeviceChangeStatus status) =>
        status switch
        {
            DeviceChangeStatus.Pending => "pending",
            DeviceChangeStatus.Approved => "approved",
            DeviceChangeStatus.Rejected => "rejected",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown device change status."),
        };

    public static bool TryParseManualStatus(string? value, out AttendanceStatus status)
    {
        status = default;
        return value?.Trim().ToLowerInvariant() switch
        {
            "present" => Set(AttendanceStatus.Present, out status),
            "late" => Set(AttendanceStatus.Late, out status),
            "absent" => Set(AttendanceStatus.Absent, out status),
            _ => false,
        };
    }

    public static bool TryParseDeviceChangeStatus(string? value, out DeviceChangeStatus status)
    {
        status = default;
        return value?.Trim().ToLowerInvariant() switch
        {
            "pending" => Set(DeviceChangeStatus.Pending, out status),
            "approved" => Set(DeviceChangeStatus.Approved, out status),
            "rejected" => Set(DeviceChangeStatus.Rejected, out status),
            _ => false,
        };
    }

    private static bool Set<TEnum>(TEnum value, out TEnum target)
        where TEnum : struct, Enum
    {
        target = value;
        return true;
    }
}
