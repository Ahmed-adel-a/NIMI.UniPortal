using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Announcements.Application.Abstractions;
using NIMI.UniPortal.Modules.Announcements.Application.Common;
using NIMI.UniPortal.Modules.Announcements.Domain;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Announcements.Application.Manage;

/// <summary>
/// Editing and deleting: an announcement the caller cannot see (or a deleted one) is a 404; one they can see but did
/// not write and do not teach is a 403. Students see only non-expired items of their own groups.
/// </summary>
internal static class AnnouncementOwnership
{
    public static async Task<Announcement> LoadManageableAsync(
        IAnnouncementsDbContext db,
        CourseAccess access,
        Guid announcementId,
        Viewer viewer,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var announcement = await db.Announcements.SingleOrDefaultAsync(a => a.Id == announcementId && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Announcement not found.");

        if (viewer.IsProfessor && await access.TeachesSubjectAsync(viewer.UserId, announcement.SubjectId, announcement.TermId, cancellationToken))
            return announcement;

        if (announcement.AuthorId == viewer.UserId)
            return announcement;

        var visible = viewer.IsAdmin;
        if (!visible && viewer.IsStudent && !announcement.IsExpiredAt(now))
        {
            var groups = await access.StudentGroupsAsync(viewer.UserId, announcement.SubjectId, announcement.TermId, cancellationToken);
            visible = groups.Count > 0 && (announcement.SubjectGroupId is null || groups.Contains(announcement.SubjectGroupId.Value));
        }

        throw visible
            ? new ForbiddenException("Only the author or a teacher of the subject can change this announcement.")
            : new NotFoundException("Announcement not found.");
    }
}
