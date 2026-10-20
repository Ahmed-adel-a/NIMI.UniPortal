using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Platform.Application;
using NIMI.UniPortal.Modules.Platform.Application.Settings.GetAdminSettings;
using NIMI.UniPortal.Modules.Platform.Application.Settings.GetPublicSettings;
using NIMI.UniPortal.Modules.Platform.Application.Settings.UpdateSettings;
using NIMI.UniPortal.Modules.Platform.Infrastructure;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Persistence;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Platform.Api;

public static class PlatformModule
{
    public static IServiceCollection AddPlatformModule(this IServiceCollection services)
    {
        services.AddPlatformApplication();
        services.AddPlatformInfrastructure(provider =>
            provider.GetRequiredService<IConfiguration>().GetConnectionString(PortalDatabase.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured."));
        return services;
    }

    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Anonymous: the login screen needs branding before anyone is authenticated (P0.1).
        endpoints.MapGet("/api/platform/settings", GetPublicSettings)
            .WithTags("Platform")
            .AllowAnonymous()
            .WithName("GetPublicSettings");

        // Student Affairs only (decision ADMINROUTE).
        var admin = endpoints.MapGroup("/api/admin/settings")
            .WithTags("Platform")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("/", GetAdminSettings).WithName("GetAdminSettings");
        admin.MapPut("/", UpdateSettings).WithName("UpdateSettings");

        return endpoints;
    }

    private static async Task<Ok<PublicSettingsDto>> GetPublicSettings(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetPublicSettingsQuery(), cancellationToken));

    private static async Task<Ok<AdminSettingsDto>> GetAdminSettings(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetAdminSettingsQuery(), cancellationToken));

    /// <summary>Full replace. Returns the saved settings with the new version (re-read after the commit).</summary>
    private static async Task<Ok<AdminSettingsDto>> UpdateSettings(UpdateSettingsCommand command, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return TypedResults.Ok(await sender.Send(new GetAdminSettingsQuery(), cancellationToken));
    }
}
