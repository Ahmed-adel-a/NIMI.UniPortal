using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.DropRegistration;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.GetMyRegistrations;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.GetOfferings;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration.RegisterForSubject;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Progress;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Progress.GetProgress;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>P4.3 — a student's offerings, self-registration and drop (decision D2, rules R1–R10).</summary>
internal static class RegistrationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/registration")
            .WithTags("Lifecycle")
            .RequireAuthorization();

        group.MapGet("/offerings", GetOfferings).WithName("GetRegistrationOfferings");
        group.MapGet("/mine", GetMyRegistrations).WithName("GetMyRegistrations");
        group.MapGet("/progress", GetProgress).WithName("GetStudentProgress");
        group.MapPost("", Register).WithName("RegisterForSubject");
        group.MapDelete("/{registrationId:guid}", Drop).WithName("DropRegistration");
    }

    private static async Task<Ok<PagedResult<OfferingDto>>> GetOfferings(
        ClaimsPrincipal caller,
        ISender sender,
        Guid? termId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetOfferingsQuery(
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Student),
                termId,
                new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Ok<MyRegistrationsDto>> GetMyRegistrations(
        ClaimsPrincipal caller,
        ISender sender,
        Guid? termId,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetMyRegistrationsQuery(
                caller.GetUserId(), caller.IsInRole(PortalRoles.Student), termId),
            cancellationToken));

    private static async Task<Ok<StudentProgressDto>> GetProgress(
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetProgressQuery(
                caller.GetUserId(), caller.IsInRole(PortalRoles.Student)),
            cancellationToken));

    private static async Task<Created<RegistrationDto>> Register(
        RegisterRequest request,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var registration = await sender.Send(
            new RegisterForSubjectCommand(
                caller.GetUserId(), caller.IsInRole(PortalRoles.Student), request.SubjectGroupId),
            cancellationToken);

        return TypedResults.Created($"/api/registration/{registration.Id}", registration);
    }

    private static async Task<NoContent> Drop(
        Guid registrationId,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DropRegistrationCommand(
                caller.GetUserId(), caller.IsInRole(PortalRoles.Student), registrationId),
            cancellationToken);

        return TypedResults.NoContent();
    }
}
