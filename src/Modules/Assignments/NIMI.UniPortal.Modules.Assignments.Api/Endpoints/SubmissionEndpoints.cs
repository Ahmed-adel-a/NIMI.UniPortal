using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Assignments.Application.Submit;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Assignments.Api.Endpoints;

/// <summary>P8.3–P8.6: submissions, receipts, review and the student's own list.</summary>
internal static class SubmissionEndpoints
{
    /// <summary>The largest allowed file (50 MB) plus room for the multipart envelope.</summary>
    private const long MaxRequestBytes = (50 * 1024 * 1024) + (1024 * 1024);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/assignments/{id:guid}/submissions", Submit)
            .WithTags("Assignments")
            .RequireAuthorization()
            .WithName("SubmitAssignment")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = MaxRequestBytes });
    }

    private static async Task<Created<SubmissionReceiptDto>> Submit(
        Guid id, IFormFile? file, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        await using var content = file?.OpenReadStream();
        var receipt = await sender.Send(
            new SubmitAssignmentCommand(id, file?.FileName, file?.ContentType, file?.Length ?? 0, content, CallerViewer.From(caller)),
            cancellationToken);
        return TypedResults.Created($"/api/submissions/{receipt.Id}", receipt);
    }
}
