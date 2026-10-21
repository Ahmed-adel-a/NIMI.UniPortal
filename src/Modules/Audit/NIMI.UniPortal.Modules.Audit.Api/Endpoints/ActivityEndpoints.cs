using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Audit.Application.Activity;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Audit.Api.Endpoints;

/// <summary>P10.2: the activity log for Student Affairs.</summary>
internal static class ActivityEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/admin/activity", List)
            .WithTags("Audit")
            .RequireAuthorization(PortalPolicies.Admin)
            .WithName("ListActivity");

    private static async Task<Ok<PagedResult<ActivityItemDto>>> List(
        Guid? actorId,
        string? entityType,
        Guid? entityId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ListActivityQuery(actorId, entityType, entityId, from, to, new PageRequest(page, pageSize)), cancellationToken));
}
