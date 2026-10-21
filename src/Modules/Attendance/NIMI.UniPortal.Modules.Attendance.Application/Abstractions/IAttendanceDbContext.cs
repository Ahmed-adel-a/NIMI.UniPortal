using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Domain.Devices;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;

namespace NIMI.UniPortal.Modules.Attendance.Application.Abstractions;

/// <summary>The Attendance data seam: handlers use this, never a repository or a SaveChanges of their own.</summary>
public interface IAttendanceDbContext
{
    DbSet<AttendanceSession> AttendanceSessions { get; }

    DbSet<AttendanceRecord> AttendanceRecords { get; }

    DbSet<RegisteredDevice> RegisteredDevices { get; }

    DbSet<DeviceChangeRequest> DeviceChangeRequests { get; }

    DbSet<AttendanceWarning> AttendanceWarnings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
