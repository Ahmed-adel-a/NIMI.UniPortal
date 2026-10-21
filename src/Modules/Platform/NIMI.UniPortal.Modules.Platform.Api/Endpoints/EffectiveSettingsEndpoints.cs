using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Platform.Application.Settings;
using NIMI.UniPortal.Modules.Platform.Application.Settings.GetEffectiveSettings;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Platform.Api.Endpoints;

/// <summary>The effective academic rules for signed-in users (P1.4); without a facultyId → the university defaults.</summary>
internal static class EffectiveSettingsEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/settings/effective", GetEffectiveSettings)
            .WithTags("Platform")
            .RequireAuthorization()
            .WithName("GetEffectiveSettings");
    }

    private static async Task<Ok<EffectiveSettingsDto>> GetEffectiveSettings(
        ISender sender, Guid? facultyId, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetEffectiveSettingsQuery(facultyId), cancellationToken));
}
