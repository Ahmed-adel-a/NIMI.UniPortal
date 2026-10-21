using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Payments.Application.Fees.GetStudentFees;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Payments.Api.Endpoints;

/// <summary>P5.2 — a student's own applicable fees and balance.</summary>
internal static class StudentFeeEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints) =>
        endpoints
            .MapGet("/api/payments/fees", GetFees)
            .WithTags("Payments")
            .RequireAuthorization()
            .WithName("GetStudentFees");

    private static async Task<Ok<StudentFeesDto>> GetFees(
        ClaimsPrincipal caller,
        ISender sender,
        Guid? termId,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new GetStudentFeesQuery(
                caller.GetUserId(),
                caller.IsInRole(PortalRoles.Student),
                termId),
            cancellationToken));
}
