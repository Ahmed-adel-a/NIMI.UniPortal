using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Scheduling.Application.Facilities;
using NIMI.UniPortal.Modules.Scheduling.Application.Facilities.CreateFacility;
using NIMI.UniPortal.Modules.Scheduling.Application.Facilities.ListFacilities;
using NIMI.UniPortal.Modules.Scheduling.Application.Facilities.UpdateFacility;
using NIMI.UniPortal.Modules.Scheduling.Domain.Facilities;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;
using NIMI.UniPortal.SharedKernel.Validation;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>P3.4 — halls, labs and other facilities, per faculty or shared by the university.</summary>
internal static class FacilityEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var read = endpoints.MapGroup("/api/facilities")
            .WithTags("Scheduling")
            .RequireAuthorization();

        read.MapGet("", ListFacilities).WithName("ListFacilities");

        var admin = endpoints.MapGroup("/api/admin/facilities")
            .WithTags("Scheduling")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapPost("", CreateFacility).WithName("CreateFacility");
        admin.MapPut("/{id:guid}", UpdateFacility).WithName("UpdateFacility");
    }

    private static async Task<Ok<PagedResult<FacilityDto>>> ListFacilities(
        ISender sender,
        Guid? facultyId,
        string? type,
        bool? includeInactive,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListFacilitiesQuery(
                facultyId,
                ParseType(type),
                includeInactive ?? false,
                new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<Created<FacilityDto>> CreateFacility(
        CreateFacilityRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var facility = await sender.Send(
            new CreateFacilityCommand(
                request.FacultyId,
                request.Name,
                request.Type,
                request.Capacity,
                request.Building,
                request.Floor),
            cancellationToken);
        return TypedResults.Created($"/api/facilities/{facility.Id}", facility);
    }

    private static async Task<Ok<FacilityDto>> UpdateFacility(
        Guid id, UpdateFacilityRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new UpdateFacilityCommand(
                id,
                request.FacultyId,
                request.Name,
                request.Type,
                request.Capacity,
                request.Building,
                request.Floor,
                request.IsActive),
            cancellationToken));

    /// <summary>Query strings bypass the JSON enum converter, so accept snake_case text and parse it here.</summary>
    private static FacilityType? ParseType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var name = value.Trim().Replace("_", string.Empty, StringComparison.Ordinal);
        if (Enum.TryParse<FacilityType>(name, true, out var type) && Enum.IsDefined(type))
            return type;

        throw ValidationErrors.For("type", "Unknown facility type.");
    }
}
