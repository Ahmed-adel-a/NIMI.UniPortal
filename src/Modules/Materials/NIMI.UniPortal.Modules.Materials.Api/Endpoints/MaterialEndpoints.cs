using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Materials.Application;
using NIMI.UniPortal.Modules.Materials.Application.Upload;
using NIMI.UniPortal.Modules.Materials.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Materials.Api.Endpoints;

/// <summary>P7.1–P7.3: course materials for the subjects a professor teaches and a student is registered in.</summary>
internal static class MaterialEndpoints
{
    /// <summary>The 50 MB file plus room for the other form fields.</summary>
    private const long MaxRequestBytes = MaterialFiles.MaxBytes + (1024 * 1024);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/materials")
            .WithTags("Materials")
            .RequireAuthorization();

        group.MapPost("/", Upload)
            .WithName("UploadMaterial")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = MaxRequestBytes });
    }

    private static async Task<Created<MaterialDto>> Upload(
        IFormFile? file,
        [FromForm] Guid subjectId,
        [FromForm] Guid? subjectGroupId,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] int? weekNumber,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await using var content = file?.OpenReadStream();
        var material = await sender.Send(
            new UploadMaterialCommand(
                subjectId,
                subjectGroupId,
                title ?? string.Empty,
                description,
                weekNumber,
                file?.FileName,
                file?.ContentType,
                file?.Length ?? 0,
                content,
                CallerViewer.From(caller)),
            cancellationToken);
        return TypedResults.Created($"/api/materials/{material.Id}", material);
    }
}
