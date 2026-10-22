using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Events.Application.Abstractions;
using NIMI.UniPortal.Modules.Events.Domain;
using NIMI.UniPortal.Modules.Users.Contracts;
using NIMI.UniPortal.SharedKernel;

namespace NIMI.UniPortal.Modules.Events.Application.Common;

/// <summary>
/// Who sees an event: Student Affairs sees every event; members see published or cancelled events for the whole
/// university or their own faculty. Anything else is 404.
/// </summary>
internal sealed class EventAudience(IEventsDbContext db, IUsersApi users)
{
    public async Task<Guid?> FacultyOfAsync(Viewer viewer, CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(viewer.UserId, cancellationToken);
        if (user?.DepartmentId is not { } departmentId)
            return null;

        return (await users.GetDepartmentAsync(departmentId, cancellationToken))?.FacultyId;
    }

    public async Task<CampusEvent> VisibleAsync(Guid eventId, Viewer viewer, CancellationToken cancellationToken)
    {
        var campusEvent = await db.Events.SingleOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");
        if (viewer.IsAdmin)
            return campusEvent;

        if (campusEvent.Status == EventStatus.Draft ||
            (campusEvent.FacultyId is { } facultyId && facultyId != await FacultyOfAsync(viewer, cancellationToken)))
            throw new NotFoundException("Event not found.");

        return campusEvent;
    }
}
