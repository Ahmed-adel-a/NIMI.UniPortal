using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Sessions.OpenSession;

/// <summary>P6.1: open the session for a slot and date (default: today in the effective timezone).</summary>
public sealed record OpenSessionCommand(Guid SlotId, DateOnly? Date, Guid OpenedBy)
    : ICommand<AttendanceSessionDto>;
