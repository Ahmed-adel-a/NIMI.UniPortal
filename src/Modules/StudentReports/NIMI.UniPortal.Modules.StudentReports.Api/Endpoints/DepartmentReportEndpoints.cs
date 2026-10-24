using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.StudentReports.Application.Answer;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Application.Inbox;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

namespace NIMI.UniPortal.Modules.StudentReports.Api.Endpoints;

/// <summary>Decision ROLES2: the department head's inbox and their answer.</summary>
internal static class DepartmentReportEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/department/reports").WithTags("Student reports").RequireAuthorization();

        group.MapGet("/", Inbox).WithName("ListDepartmentStudentReports");
        group.MapPost("/{id:guid}/answer", Answer).WithName("AnswerStudentReport");
    }

    /// <param name="status">open, answered or closed.</param>
    private static async Task<Ok<PagedResult<StudentReportDto>>> Inbox(
        string? status, int? page, int? pageSize, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new DepartmentReportsQuery(CallerViewer.From(caller), status, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<StudentReportDto>> Answer(
        Guid id, AnswerReportRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new AnswerReportCommand(CallerViewer.From(caller), id, request.Reply, request.Close), cancellationToken));
}
