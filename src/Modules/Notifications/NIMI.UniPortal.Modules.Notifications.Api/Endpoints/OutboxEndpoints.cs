using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Notifications.Application.Emails;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Paging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Notifications.Api.Endpoints;

/// <summary>
/// Development only (or <c>Email:ExposeOutbox</c>): the email outbox for Student Affairs, so password-reset links can
/// be followed without a mail server.
/// </summary>
internal static class OutboxEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/admin/dev/emails", List)
            .WithTags("Notifications")
            .RequireAuthorization(PortalPolicies.Admin)
            .WithName("ListOutboxEmails");
    }

    private static async Task<Ok<PagedResult<OutboundEmailDto>>> List(
        string? to, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new ListOutboxQuery(to, new PageRequest(page, pageSize)), cancellationToken));
}
