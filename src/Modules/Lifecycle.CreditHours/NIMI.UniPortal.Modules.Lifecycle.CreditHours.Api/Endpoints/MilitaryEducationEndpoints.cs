using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.ApplyForMilitaryEducation;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.AssignMilitaryEducation;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.ResyncMilitaryEducation;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>
/// P5.5 — military education (MILEDU, D5): a student applies in opt-in mode, Student Affairs assigns in any mode,
/// and the resync endpoint recovers registrations created before their fee item existed.
/// </summary>
internal static class MilitaryEducationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var student = endpoints.MapGroup("/api/military-education")
            .WithTags("Lifecycle")
            .RequireAuthorization();

        student.MapPost("/apply", Apply).WithName("ApplyForMilitaryEducation");

        var admin = endpoints.MapGroup("/api/admin/military-education")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapPost("/assign", Assign).WithName("AssignMilitaryEducation");
        admin.MapPost("/resync", Resync).WithName("ResyncMilitaryEducation");
    }

    private static async Task<Created<MilitaryEducationApplicationDto>> Apply(
        ApplyForMilitaryEducationRequest request,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var application = await sender.Send(
            new ApplyForMilitaryEducationCommand(
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Student),
                request.SubjectGroupId),
            cancellationToken);

        return TypedResults.Created($"/api/military-education/{application.RegistrationId}", application);
    }

    private static async Task<Ok<MilitaryEducationAssignmentDto>> Assign(
        AssignMilitaryEducationRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new AssignMilitaryEducationCommand(
                request.StudentIds ?? [],
                request.SubjectGroupId),
            cancellationToken));

    private static async Task<Ok<MilitaryEducationResyncDto>> Resync(
        Guid? termId,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ResyncMilitaryEducationCommand(termId ?? Guid.Empty),
            cancellationToken));
}
