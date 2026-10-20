using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Users.Application.Organization;
using NIMI.UniPortal.Modules.Users.Application.Organization.CreateDepartment;
using NIMI.UniPortal.Modules.Users.Application.Organization.CreateFaculty;
using NIMI.UniPortal.Modules.Users.Application.Organization.ListDepartments;
using NIMI.UniPortal.Modules.Users.Application.Organization.ListFaculties;
using NIMI.UniPortal.Modules.Users.Application.Organization.UpdateDepartment;
using NIMI.UniPortal.Modules.Users.Application.Organization.UpdateFaculty;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Users.Api.Endpoints;

/// <summary>Faculties + departments (US-A16, US-A11 part 1). Reads: any signed-in user. Writes: Student Affairs.</summary>
internal static class OrganizationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var read = endpoints.MapGroup("/api").WithTags("Organization").RequireAuthorization();
        read.MapGet("/faculties", ListFaculties).WithName("ListFaculties");
        read.MapGet("/departments", ListDepartments).WithName("ListDepartments");

        var admin = endpoints.MapGroup("/api/admin").WithTags("Organization").RequireAuthorization(PortalPolicies.Admin);
        admin.MapPost("/faculties", CreateFaculty).WithName("CreateFaculty");
        admin.MapPut("/faculties/{id:guid}", UpdateFaculty).WithName("UpdateFaculty");
        admin.MapPost("/departments", CreateDepartment).WithName("CreateDepartment");
        admin.MapPut("/departments/{id:guid}", UpdateDepartment).WithName("UpdateDepartment");
    }

    private static async Task<Ok<PagedResult<FacultyDto>>> ListFaculties(
        ISender sender, bool? includeInactive, int? page, int? pageSize, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListFacultiesQuery(includeInactive ?? false, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Ok<PagedResult<DepartmentDto>>> ListDepartments(
        ISender sender, Guid? facultyId, bool? includeInactive, int? page, int? pageSize, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListDepartmentsQuery(facultyId, includeInactive ?? false, new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<Created<FacultyDto>> CreateFaculty(CreateFacultyCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var faculty = await sender.Send(command, cancellationToken);
        return TypedResults.Created($"/api/faculties?code={faculty.Code}", faculty);
    }

    private static async Task<Ok<FacultyDto>> UpdateFaculty(
        Guid id, UpdateFacultyRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateFacultyCommand(id, request.Code, request.Name, request.NameAr, request.IsActive), cancellationToken));

    private static async Task<Created<DepartmentDto>> CreateDepartment(CreateDepartmentCommand command, ISender sender, CancellationToken cancellationToken)
    {
        var department = await sender.Send(command, cancellationToken);
        return TypedResults.Created($"/api/departments?facultyId={department.FacultyId}", department);
    }

    private static async Task<Ok<DepartmentDto>> UpdateDepartment(
        Guid id, UpdateDepartmentRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateDepartmentCommand(id, request.FacultyId, request.Code, request.Name, request.NameAr, request.Description, request.IsActive),
            cancellationToken));
}
