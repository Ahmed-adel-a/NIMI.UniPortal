using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.Attendance.Application.Reports.History;

/// <summary>P6.6: the signed-in student's own attendance records with a percentage per subject.</summary>
public sealed record GetAttendanceHistoryQuery(
    Guid StudentId,
    bool IsStudent,
    Guid? SubjectId,
    Guid? TermId,
    PageRequest Paging) : IQuery<AttendanceHistoryDto>;
