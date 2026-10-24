using System.Security.Claims;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Grades.Api.Endpoints;

/// <summary>Turns the signed-in principal into the <see cref="Viewer"/> the handlers authorize against.</summary>
internal static class CallerViewer
{
    public static Viewer From(ClaimsPrincipal caller) =>
        new(caller.GetUserId(),
            caller.IsInRole(PortalRoles.Admin),
            caller.IsTeachingStaff(),
            caller.IsInRole(PortalRoles.Student));
}
