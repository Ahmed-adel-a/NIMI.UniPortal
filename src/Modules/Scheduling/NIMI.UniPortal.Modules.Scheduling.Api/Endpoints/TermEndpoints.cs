using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Scheduling.Application.Terms;
using NIMI.UniPortal.Modules.Scheduling.Application.Terms.ActivateTerm;
using NIMI.UniPortal.Modules.Scheduling.Application.Terms.CloseTerm;
using NIMI.UniPortal.Modules.Scheduling.Application.Terms.CreateTerm;
using NIMI.UniPortal.Modules.Scheduling.Application.Terms.GetCurrentTerm;
using NIMI.UniPortal.Modules.Scheduling.Application.Terms.ListTerms;
using NIMI.UniPortal.Modules.Scheduling.Application.Terms.UpdateTerm;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>
/// P3.1 — academic terms (rules R1, R2). Reading is open to every signed-in user; managing is admin-only.
/// </summary>
internal static class TermEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var read = endpoints.MapGroup("/api/terms")
            .WithTags("Scheduling")
            .RequireAuthorization();

        read.MapGet("", ListTerms).WithName("ListTerms");
        read.MapGet("/current", GetCurrentTerm).WithName("GetCurrentTerm");

        var admin = endpoints.MapGroup("/api/admin/terms")
            .WithTags("Scheduling")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapPost("", CreateTerm).WithName("CreateTerm");
        admin.MapPut("/{id:guid}", UpdateTerm).WithName("UpdateTerm");
        admin.MapPost("/{id:guid}/activate", ActivateTerm).WithName("ActivateTerm");
        admin.MapPost("/{id:guid}/close", CloseTerm).WithName("CloseTerm");
    }

    private static async Task<Ok<PagedResult<TermDto>>> ListTerms(
        ISender sender, int? page, int? pageSize, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListTermsQuery(new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<TermDto>> GetCurrentTerm(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetCurrentTermQuery(), cancellationToken));

    private static async Task<Created<TermDto>> CreateTerm(
        CreateTermRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var term = await sender.Send(
            new CreateTermCommand(request.AcademicYear, request.Number, request.StartsOn, request.EndsOn, request.WeekCount),
            cancellationToken);
        return TypedResults.Created($"/api/terms/{term.Id}", term);
    }

    private static async Task<Ok<TermDto>> UpdateTerm(
        Guid id, UpdateTermRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateTermCommand(id, request.AcademicYear, request.Number, request.StartsOn, request.EndsOn, request.WeekCount),
            cancellationToken));

    private static async Task<Ok<TermDto>> ActivateTerm(
        Guid id, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ActivateTermCommand(id), cancellationToken));

    private static async Task<Ok<TermDto>> CloseTerm(
        Guid id, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new CloseTermCommand(id), cancellationToken));
}
