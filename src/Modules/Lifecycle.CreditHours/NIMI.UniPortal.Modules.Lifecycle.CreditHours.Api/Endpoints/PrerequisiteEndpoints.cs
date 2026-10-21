using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites.GetSubjectPrerequisites;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites.SetSubjectPrerequisites;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>P4.2 — subject prerequisites, managed by Student Affairs (decision PREREQ).</summary>
internal static class PrerequisiteEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/registration/prerequisites/{subjectId:guid}")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("", Get).WithName("GetSubjectPrerequisites");
        admin.MapPut("", Set).WithName("SetSubjectPrerequisites");
    }

    private static async Task<Ok<SubjectPrerequisitesDto>> Get(
        Guid subjectId, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetSubjectPrerequisitesQuery(subjectId), cancellationToken));

    private static async Task<Ok<SubjectPrerequisitesDto>> Set(
        Guid subjectId,
        SetPrerequisitesRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new SetSubjectPrerequisitesCommand(subjectId, request.PrerequisiteSubjectIds ?? []),
            cancellationToken));
}
