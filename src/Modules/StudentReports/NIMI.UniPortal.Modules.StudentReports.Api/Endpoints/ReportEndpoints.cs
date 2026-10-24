using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Application.Detail;
using NIMI.UniPortal.Modules.StudentReports.Application.Mine;
using NIMI.UniPortal.Modules.StudentReports.Application.Submit;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.StudentReports.Api.Endpoints;

/// <summary>Decision ROLES2: students send and follow their reports; one report is readable by its student, head or Student Affairs.</summary>
internal static class ReportEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/reports").WithTags("Student reports").RequireAuthorization();

        group.MapPost("/", Submit).WithName("SubmitStudentReport");
        group.MapGet("/mine", Mine).WithName("ListMyStudentReports");
        group.MapGet("/{id:guid}", Get).WithName("GetStudentReport");
    }

    private static async Task<Created<ReportDto>> Submit(
        SubmitReportRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var report = await sender.Send(
            new SubmitReportCommand(CallerViewer.From(caller), request.Category, request.Title, request.Message), cancellationToken);
        return TypedResults.Created($"/api/reports/{report.Id}", report);
    }

    private static async Task<Ok<PagedResult<ReportDto>>> Mine(
        int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new MyReportsQuery(CallerViewer.From(caller), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<ReportDto>> Get(Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetReportQuery(CallerViewer.From(caller), id), cancellationToken));
}
