using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Attendance.Application.Abstractions;
using NIMI.UniPortal.Modules.Attendance.Domain.Devices;
using NIMI.UniPortal.Modules.Attendance.Domain.Records;
using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;
using NIMI.UniPortal.SharedKernel;
using Npgsql;

namespace NIMI.UniPortal.Modules.Attendance.Infrastructure.Persistence;

public sealed class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
    : DbContext(options), IAttendanceDbContext
{
    public const string Schema = "attendance";

    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<RegisteredDevice> RegisteredDevices => Set<RegisteredDevice>();

    public DbSet<DeviceChangeRequest> DeviceChangeRequests => Set<DeviceChangeRequest>();

    public DbSet<AttendanceWarning> AttendanceWarnings => Set<AttendanceWarning>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceDbContext).Assembly);
    }

    /// <summary>
    /// Turns the module's unique indexes into the API's precise 409 messages — including the check-in race
    /// that the application-level duplicate check cannot see (rule R4).
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
                  {
                      SqlState: PostgresErrorCodes.UniqueViolation,
                  } postgres)
        {
            throw new ConflictException(postgres.ConstraintName switch
            {
                "ux_attendance_records_session_student" => "Already checked in.",
                "ux_attendance_sessions_slot_date" => "A session already exists for this slot and date.",
                "ux_registered_devices_student_active" => "Use your registered device.",
                "ux_device_change_requests_student_pending" => "A device change request is already pending.",
                _ => "A record with the same unique value already exists.",
            });
        }
    }
}
