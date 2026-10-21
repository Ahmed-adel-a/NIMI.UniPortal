using NIMI.UniPortal.Modules.Attendance.Domain.Sessions;
using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Attendance.Application.Common;

/// <summary>Rule R6: only the slot's teaching staff open or run a session.</summary>
internal static class AttendanceAccess
{
    public static async Task EnsureTeachingAsync(
        ISchedulingApi scheduling,
        AttendanceSession session,
        Guid staffId,
        CancellationToken cancellationToken)
    {
        var slot = await scheduling.GetSlotAsync(session.SlotId, cancellationToken);
        var teaches = slot?.StaffId == staffId ||
                      await scheduling.IsTeachingAsync(
                          staffId,
                          session.SubjectId,
                          session.TermId,
                          session.SubjectGroupId,
                          cancellationToken);

        if (!teaches)
            throw new ForbiddenException("You do not teach this session.");
    }
}
