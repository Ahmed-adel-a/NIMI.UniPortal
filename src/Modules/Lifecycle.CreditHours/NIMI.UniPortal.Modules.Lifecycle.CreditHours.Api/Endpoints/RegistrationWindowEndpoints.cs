using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.CreateRegistrationWindow;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.ListRegistrationWindows;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.RegistrationWindows.UpdateRegistrationWindow;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api.Endpoints;

/// <summary>P4.2 — registration windows: one per term, with that term's credit limits (decision PREREQ).</summary>
internal static class RegistrationWindowEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/registration-windows")
            .WithTags("Lifecycle")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("", List).WithName("ListRegistrationWindows");
        admin.MapPost("", Create).WithName("CreateRegistrationWindow");
        admin.MapPut("/{id:guid}", Update).WithName("UpdateRegistrationWindow");
    }

    private static async Task<Ok<PagedResult<RegistrationWindowDto>>> List(
        ISender sender, int? page, int? pageSize, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListRegistrationWindowsQuery(new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Created<RegistrationWindowDto>> Create(
        CreateRegistrationWindowRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var window = await sender.Send(
            new CreateRegistrationWindowCommand(
                request.TermId, request.OpensAt, request.ClosesAt, request.MinCredits, request.MaxCredits),
            cancellationToken);

        return TypedResults.Created($"/api/admin/registration-windows/{window.Id}", window);
    }

    private static async Task<Ok<RegistrationWindowDto>> Update(
        Guid id,
        UpdateRegistrationWindowRequest request,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateRegistrationWindowCommand(
                id, request.OpensAt, request.ClosesAt, request.MinCredits, request.MaxCredits),
            cancellationToken));
}
