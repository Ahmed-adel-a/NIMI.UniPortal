using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Progress;
using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Progress.GetProgress;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

/// <summary>The student's progress at the same route and shape as CreditHours (one frontend screen for both).</summary>
internal static class ProgressEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/registration/progress", GetProgress)
            .WithTags("Lifecycle")
            .RequireAuthorization()
            .WithName("GetStudentProgress");
    }

    private static async Task<Ok<StudentProgressDto>> GetProgress(
        ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetProgressQuery(caller.GetUserId(), caller.IsInRole(PortalRoles.Student)), cancellationToken));
}
