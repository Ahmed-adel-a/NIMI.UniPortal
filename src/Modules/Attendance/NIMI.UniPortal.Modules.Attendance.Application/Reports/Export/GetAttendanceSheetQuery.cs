using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.Export;

/// <summary>P10.4: the group's attendance sheet (its teaching staff or Student Affairs).</summary>
public sealed record GetAttendanceSheetQuery(Guid GroupId, Guid CallerId, bool IsAdmin) : IQuery<AttendanceSheetDto>;
