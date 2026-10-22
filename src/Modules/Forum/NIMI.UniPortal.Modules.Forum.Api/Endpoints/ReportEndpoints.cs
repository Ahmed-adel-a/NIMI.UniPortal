using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.Modules.Forum.Application.Reports;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>Reports: any member files one; Student Affairs works the queue.</summary>
internal static class ReportEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/forum/reports", File)
            .WithTags("Forum").RequireAuthorization().WithName("ReportForumContent");

        var admin = endpoints.MapGroup("/api/admin/forum/reports")
            .WithTags("Forum")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapGet("/", List).WithName("ListForumReports");
        admin.MapPost("/{id:guid}/resolve", Resolve).WithName("ResolveForumReport");
    }

    private static async Task<Created<ReportDto>> File(
        ReportRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var report = await sender.Send(
            new FileReportCommand(request.ThreadId, request.PostId, request.Reason, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.Created($"/api/admin/forum/reports/{report.Id}", report);
    }

    private static async Task<Ok<PagedResult<ReportDto>>> List(
        string? status, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListReportsQuery(status, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<ReportDto>> Resolve(
        Guid id, ResolveRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ResolveReportCommand(id, request.Action, CallerViewer.From(caller)), cancellationToken));
}
