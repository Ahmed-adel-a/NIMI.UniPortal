using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Platform.Application.AcademicSystems;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Platform.Api.Endpoints;

/// <summary>
/// Decision LIFECYCLEDEPT: the academic system (credit hours or year groups) per department, faculty or university.
/// Anyone signed in may read it; Student Affairs sets it.
/// </summary>
internal static class AcademicSystemEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/settings/academic-system", Get)
            .WithTags("Platform")
            .RequireAuthorization()
            .WithName("GetAcademicSystem");

        var admin = endpoints.MapGroup("/api/admin")
            .WithTags("Platform")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapGet("/academic-systems", Overview).WithName("GetAcademicSystemsOverview");
        admin.MapPut("/academic-system", SetUniversity).WithName("SetUniversityAcademicSystem");
        admin.MapPut("/faculties/{facultyId:guid}/academic-system", SetFaculty).WithName("SetFacultyAcademicSystem");
        admin.MapPut("/departments/{departmentId:guid}/academic-system", SetDepartment).WithName("SetDepartmentAcademicSystem");
    }

    private static async Task<Ok<AcademicSystemDto>> Get(Guid? departmentId, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetAcademicSystemQuery(departmentId), cancellationToken));

    private static async Task<Ok<AcademicSystemsOverviewDto>> Overview(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetAcademicSystemsOverviewQuery(), cancellationToken));

    private static async Task<Ok<AcademicSystemsOverviewDto>> SetUniversity(
        AcademicSystemRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new SetAcademicSystemCommand(null, null, request.AcademicSystem), cancellationToken));

    private static async Task<Ok<AcademicSystemsOverviewDto>> SetFaculty(
        Guid facultyId, AcademicSystemRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new SetAcademicSystemCommand(facultyId, null, request.AcademicSystem), cancellationToken));

    private static async Task<Ok<AcademicSystemsOverviewDto>> SetDepartment(
        Guid departmentId, AcademicSystemRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new SetAcademicSystemCommand(null, departmentId, request.AcademicSystem), cancellationToken));
}
