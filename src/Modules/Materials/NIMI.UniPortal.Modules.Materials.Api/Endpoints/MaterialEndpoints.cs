using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Materials.Application;
using NIMI.UniPortal.Modules.Materials.Application.Browse;
using NIMI.UniPortal.Modules.Materials.Application.Manage;
using NIMI.UniPortal.Modules.Materials.Application.Upload;
using NIMI.UniPortal.Modules.Materials.Domain;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;

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
        group.MapGet("/", List).WithName("ListMaterials");
        group.MapGet("/{id:guid}/download", Download).WithName("DownloadMaterial");
        group.MapPut("/{id:guid}", Edit).WithName("EditMaterial");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteMaterial");
    }

    private static async Task<Ok<MaterialDto>> Edit(
        Guid id,
        EditMaterialRequest request,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new EditMaterialCommand(id, request.Title ?? string.Empty, request.Description, request.WeekNumber, CallerViewer.From(caller)),
            cancellationToken));

    private static async Task<NoContent> Delete(
        Guid id,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteMaterialCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResult<MaterialDto>>> List(
        Guid? subjectId,
        int? weekNumber,
        string? type,
        int? page,
        int? pageSize,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListMaterialsQuery(subjectId, weekNumber, type, CallerViewer.From(caller), new PageRequest(page, pageSize)),
            cancellationToken));

    private static async Task<FileStreamHttpResult> Download(
        Guid id,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var file = await sender.Send(new DownloadMaterialCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.File(file.Content, file.ContentType, file.FileName);
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
