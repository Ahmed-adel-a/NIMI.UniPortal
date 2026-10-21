using NIMI.UniPortal.Abstractions.Lifecycle;
using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Modules.Attendance.Application.Common;

/// <summary>The group's confirmed members (lifecycle owns rosters — ENROLL), with name and code, ordered by student code.</summary>
internal static class GroupMembers
{
    public static async Task<IReadOnlyList<MemberProfile>> LoadAsync(
        IAcademicLifecycle lifecycle,
        IUsersApi users,
        Guid subjectGroupId,
        CancellationToken cancellationToken)
    {
        var studentIds = await lifecycle.GetStudentsInGroupAsync(subjectGroupId, cancellationToken);

        var members = new List<MemberProfile>(studentIds.Count);
        foreach (var studentId in studentIds)
        {
            var user = await users.GetUserAsync(studentId, cancellationToken);
            members.Add(new MemberProfile(studentId, user?.FullName ?? string.Empty, user?.StudentCode));
        }

        return [.. members.OrderBy(m => m.StudentCode, StringComparer.Ordinal).ThenBy(m => m.StudentId)];
    }
}
