using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Hosting.Dashboards;
using NIMI.UniPortal.Hosting.DemoData;
using NIMI.UniPortal.Hosting.Errors;
using NIMI.UniPortal.Hosting.FrontendAccess;
using NIMI.UniPortal.Hosting.Health;
using NIMI.UniPortal.Hosting.Security;
using NIMI.UniPortal.Hosting.Storage;
using NIMI.UniPortal.Modules.Announcements.Api;
using NIMI.UniPortal.Modules.Assignments.Api;
using NIMI.UniPortal.Modules.Attendance.Api;
using NIMI.UniPortal.Modules.Audit.Api;
using NIMI.UniPortal.Modules.Grades.Api;
using NIMI.UniPortal.Modules.Materials.Api;
using NIMI.UniPortal.Modules.Notifications.Api;
using NIMI.UniPortal.Modules.Payments.Api;
using NIMI.UniPortal.Modules.Platform.Api;
using NIMI.UniPortal.Modules.Scheduling.Api;
using NIMI.UniPortal.Modules.Users.Api;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Hosting;

/// <summary>
/// What every deployment composes: cross-cutting services, the core modules and the dashboards. A host's Program adds
/// its one lifecycle variant and the premium modules it sells — between <see cref="AddPortalCore"/> and Build, and
/// between <see cref="UsePortalCoreAsync"/> and Run.
/// </summary>
public static class PortalCore
{
    public static WebApplicationBuilder AddPortalCore(this WebApplicationBuilder builder)
    {
        // ── Cross-cutting ──────────────────────────────────────────────
        builder.Services.AddSingleton(TimeProvider.System);                       // the only clock (DateTime.Now is banned)
        builder.Services.AddMessaging();                                          // before modules: validation runs outermost
        builder.Services.AddEventBus();                                           // publish-after-commit; modules register their handlers
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddProblemDetails();   // required by UseExceptionHandler(); our handler writes the actual envelope
        builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);   // malformed bodies → our envelope
        builder.Services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));
        builder.AddPortalAuthentication();                                        // JWT validation + policies (Security/)
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();              // who acted, for the activity log (P10.2)
        builder.Services.AddFileStorage(builder.Configuration, builder.Environment); // uploads on local disk (Storage/)
        builder.AddFrontendCors();                                                // Cors:AllowedOrigins (FrontendAccess/)
        builder.AddApiDocs();                                                     // /openapi/v1.json + /docs

        // ── Core modules ───────────────────────────────────────────────
        builder.Services.AddPlatformModule();
        builder.Services.AddUsersModule();
        builder.Services.AddNotificationsModule(builder.Configuration, builder.Environment);
        builder.Services.AddSchedulingModule();
        builder.Services.AddAttendanceModule();
        builder.Services.AddMaterialsModule();
        builder.Services.AddAnnouncementsModule();
        builder.Services.AddAssignmentsModule();
        builder.Services.AddGradesModule();
        builder.Services.AddAuditModule();
        builder.Services.AddPaymentsModule(builder.Configuration, builder.Environment);
        builder.Services.AddDashboards();                // P10: host-level dashboards over module contracts
        builder.Services.AddDemoData(builder.Configuration);   // Development only, Seed:DemoData (DEVDOCKER)

        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [DatabaseHealthCheck.Tag], timeout: TimeSpan.FromSeconds(5));
        return builder;
    }

    public static async Task<WebApplication> UsePortalCoreAsync(this WebApplication app)
    {
        await app.ApplyModuleMigrationsAsync();
        await app.RunDevelopmentSeedersAsync();

        app.UseExceptionHandler();
        app.UseFrontendCors();
        app.UseAuthentication();
        app.UseAuthorization();

        // Process alive: runs no checks.
        app.MapHealthChecks("/api/health", new HealthCheckOptions { Predicate = _ => false });
        // Database reachable: 200 Healthy / 503 Unhealthy.
        app.MapHealthChecks("/api/health/db", new HealthCheckOptions { Predicate = c => c.Tags.Contains(DatabaseHealthCheck.Tag) });

        app.MapPlatformEndpoints();
        app.MapUsersEndpoints();
        app.MapNotificationsEndpoints();
        app.MapSchedulingEndpoints();
        app.MapAttendanceEndpoints();
        app.MapMaterialsEndpoints();
        app.MapAnnouncementsEndpoints();
        app.MapAssignmentsEndpoints();
        app.MapGradesEndpoints();
        app.MapAuditEndpoints();
        app.MapPaymentsEndpoints();
        app.MapDashboardEndpoints();
        app.MapApiDocs();
        return app;
    }
}
