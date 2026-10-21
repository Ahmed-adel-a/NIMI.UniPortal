using NIMI.UniPortal.Modules.Scheduling.Contracts;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Attendance.Application.Common;

/// <summary>Group-level reports: Student Affairs sees every group, a professor only the groups they teach.</summary>
internal static class GroupAccess
{
    public static async Task<GroupRef> EnsureCanSeeAsync(
        ISchedulingApi scheduling,
        Guid groupId,
        Guid callerId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var group = await scheduling.GetGroupAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Group not found.");

        if (isAdmin)
            return group;

        var teaches = await scheduling.IsTeachingAsync(callerId, group.SubjectId, group.TermId, group.Id, cancellationToken);
        if (!teaches)
            throw new ForbiddenException("You do not teach this group.");

        return group;
    }
}
