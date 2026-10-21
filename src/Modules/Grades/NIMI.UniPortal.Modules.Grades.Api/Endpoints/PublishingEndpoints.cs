using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Grades.Application.Publishing;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Grades.Api.Endpoints;

/// <summary>P9.2: Student Affairs confirms groups and publishes terms (decision D3).</summary>
internal static class PublishingEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/grades")
            .WithTags("Grades")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapPost("/confirm", Confirm).WithName("ConfirmGroupGrades");
        admin.MapPost("/publish", Publish).WithName("PublishGrades");
    }

    private static async Task<Ok<ConfirmResultDto>> Confirm(
        ConfirmGradesRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ConfirmGroupGradesCommand(request.GroupId, CallerViewer.From(caller)), cancellationToken));

    private static async Task<Ok<PublishResultDto>> Publish(
        PublishGradesRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new PublishGradesCommand(request.TermId, request.SubjectId, request.GroupId, CallerViewer.From(caller)),
            cancellationToken));
}
