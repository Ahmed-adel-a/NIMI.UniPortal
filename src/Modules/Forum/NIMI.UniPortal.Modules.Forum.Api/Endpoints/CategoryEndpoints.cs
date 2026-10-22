using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Forum.Application.Categories;
using NIMI.UniPortal.Modules.Forum.Application.Common;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Forum.Api.Endpoints;

/// <summary>Forum categories: everyone lists what they can use; Student Affairs manages them.</summary>
internal static class CategoryEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/forum/categories", List)
            .WithTags("Forum").RequireAuthorization().WithName("ListForumCategories");

        var admin = endpoints.MapGroup("/api/admin/forum/categories")
            .WithTags("Forum")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapPost("/", Create).WithName("CreateForumCategory");
        admin.MapPut("/{id:guid}", Update).WithName("UpdateForumCategory");
    }

    private static async Task<Ok<IReadOnlyList<CategoryDto>>> List(ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListCategoriesQuery(CallerViewer.From(caller)), cancellationToken));

    private static async Task<Created<CategoryDto>> Create(CategoryRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var category = await sender.Send(
            new SaveCategoryCommand(null, request.Name, request.NameAr, request.FacultyId, request.SortOrder, true), cancellationToken);
        return TypedResults.Created($"/api/admin/forum/categories/{category.Id}", category);
    }

    private static async Task<Ok<CategoryDto>> Update(Guid id, CategoryRequest request, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new SaveCategoryCommand(id, request.Name, request.NameAr, request.FacultyId, request.SortOrder, request.IsActive ?? true),
            cancellationToken));
}
