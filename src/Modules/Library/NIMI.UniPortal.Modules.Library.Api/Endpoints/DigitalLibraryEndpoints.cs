using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Library.Application.Digital;
using NIMI.UniPortal.Modules.Library.Domain.Digital;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Library.Api.Endpoints;

/// <summary>
/// Decision DIGILIB: Student Affairs adds verified e-books for the university, a faculty or a department; members
/// browse and download the ones meant for them.
/// </summary>
internal static class DigitalLibraryEndpoints
{
    /// <summary>The 100 MB file plus room for the other form fields.</summary>
    private const long MaxRequestBytes = DigitalBookFiles.MaxBytes + (1024 * 1024);

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/library/digital")
            .WithTags("Library")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapPost("", Add)
            .WithName("AddDigitalBook")
            .DisableAntiforgery()   // bearer-token API, no cookies
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = MaxRequestBytes });
        admin.MapPut("/{id:guid}", Edit).WithName("EditDigitalBook");
        admin.MapDelete("/{id:guid}", Remove).WithName("RemoveDigitalBook");

        var library = endpoints.MapGroup("/api/library/digital")
            .WithTags("Library")
            .RequireAuthorization();
        library.MapGet("", List).WithName("ListDigitalBooks");
        library.MapGet("/{id:guid}/download", Download).WithName("DownloadDigitalBook");
    }

    private static async Task<Created<DigitalBookDto>> Add(
        IFormFile? file,
        [FromForm] string? title,
        [FromForm] string? authors,
        [FromForm] string? description,
        [FromForm] Guid? facultyId,
        [FromForm] Guid? departmentId,
        [FromForm] Guid? subjectId,
        ClaimsPrincipal caller,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await using var content = file?.OpenReadStream();
        var book = await sender.Send(
            new AddDigitalBookCommand(title, authors, description, facultyId, departmentId, subjectId, file?.FileName,
                file?.Length ?? 0, content, caller.GetUserId()),
            cancellationToken);
        return TypedResults.Created($"/api/library/digital/{book.Id}/download", book);
    }

    private static async Task<Ok<DigitalBookDto>> Edit(
        Guid id, DigitalBookRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new EditDigitalBookCommand(id, request.Title, request.Authors, request.Description, request.FacultyId,
                request.DepartmentId, request.SubjectId),
            cancellationToken));

    private static async Task<NoContent> Remove(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveDigitalBookCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResult<DigitalBookDto>>> List(
        string? search, Guid? subjectId, int? page, int? pageSize, ClaimsPrincipal caller, ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListDigitalBooksQuery(search, subjectId, CallerViewer.From(caller), new PageRequest(page, pageSize)), cancellationToken));

    private static async Task<FileStreamHttpResult> Download(
        Guid id, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var file = await sender.Send(new DownloadDigitalBookCommand(id, CallerViewer.From(caller)), cancellationToken);
        return TypedResults.File(file.Content, file.ContentType, file.FileName);
    }
}
