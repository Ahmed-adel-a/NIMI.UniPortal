using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.StudentReports.Application.Common;
using NIMI.UniPortal.Modules.StudentReports.Application.Oversight;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.StudentReports.Api.Endpoints;

/// <summary>Decision ROLES2: Student Affairs reads every report (oversight, departments without a head).</summary>
internal static class AdminReportEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/reports").WithTags("Student reports").RequireAuthorization(PortalPolicies.Admin);

        group.MapGet("/", List).WithName("ListAllStudentReports");
    }

    /// <param name="status">open, answered or closed.</param>
    private static async Task<Ok<PagedResult<ReportDto>>> List(
        Guid? departmentId, string? status, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new AllReportsQuery(departmentId, status, new PageRequest(page, pageSize)), cancellationToken));
}
