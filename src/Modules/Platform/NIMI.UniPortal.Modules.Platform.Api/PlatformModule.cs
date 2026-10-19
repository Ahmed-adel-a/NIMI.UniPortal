using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIMI.UniPortal.Modules.Platform.Application;
using NIMI.UniPortal.Modules.Platform.Application.Settings.GetPublicSettings;
using NIMI.UniPortal.Modules.Platform.Infrastructure;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Persistence;

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
        var platform = endpoints.MapGroup("/api/platform").WithTags("Platform");

        // Anonymous: the login screen needs branding before anyone is authenticated (P0.1).
        platform.MapGet("/settings", GetPublicSettings)
            .AllowAnonymous()
            .WithName("GetPublicSettings");

        return endpoints;
    }

    private static async Task<Ok<PublicSettingsDto>> GetPublicSettings(ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetPublicSettingsQuery(), cancellationToken));
}
