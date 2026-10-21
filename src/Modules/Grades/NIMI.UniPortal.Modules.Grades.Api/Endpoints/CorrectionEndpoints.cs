using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Grades.Application.Corrections;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Grades.Api.Endpoints;

/// <summary>P9 corrections: Student Affairs changes a published grade (R4) and reads its audit trail.</summary>
internal static class CorrectionEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/grades/{id:guid}")
            .WithTags("Grades")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapPost("/correct", Correct).WithName("CorrectGrade");
        admin.MapGet("/corrections", Corrections).WithName("ListGradeCorrections");
    }

    private static async Task<Ok<FinalGradeDto>> Correct(
        Guid id, CorrectGradeRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new CorrectGradeCommand(id, request.Score, request.Reason, CallerViewer.From(caller)), cancellationToken));

    private static async Task<Ok<IReadOnlyList<GradeCorrectionDto>>> Corrections(
        Guid id, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListCorrectionsQuery(id), cancellationToken));
}
