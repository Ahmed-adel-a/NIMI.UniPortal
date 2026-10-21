using System.Text.Json;
using System.Text.Json.Serialization;
using Host.CairoUniversity;
using Host.CairoUniversity.Errors;
using Host.CairoUniversity.Health;
using Host.CairoUniversity.Security;
using Host.CairoUniversity.Storage;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NIMI.UniPortal.EventBus;
using NIMI.UniPortal.Modules.Announcements.Api;
using NIMI.UniPortal.Modules.Attendance.Api;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api;
using NIMI.UniPortal.Modules.Materials.Api;
using NIMI.UniPortal.Modules.Notifications.Api;
using NIMI.UniPortal.Modules.Payments.Api;
using NIMI.UniPortal.Modules.Platform.Api;
using NIMI.UniPortal.Modules.Scheduling.Api;
using NIMI.UniPortal.Modules.Users.Api;
using NIMI.UniPortal.SharedKernel.Messaging;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddFileStorage(builder.Configuration, builder.Environment); // uploads on local disk (Storage/)

// ── Core modules ───────────────────────────────────────────────
builder.Services.AddPlatformModule();
builder.Services.AddUsersModule();
builder.Services.AddNotificationsModule();
builder.Services.AddSchedulingModule();
builder.Services.AddAttendanceModule();
builder.Services.AddMaterialsModule();
builder.Services.AddAnnouncementsModule();
builder.Services.AddPaymentsModule(builder.Configuration, builder.Environment);
builder.Services.AddCreditHoursModule();   // the one lifecycle variant this deployment composes

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: [DatabaseHealthCheck.Tag], timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

await app.ApplyModuleMigrationsAsync();
await app.RunDevelopmentSeedersAsync();

app.UseExceptionHandler();
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
app.MapPaymentsEndpoints();
app.MapCreditHoursEndpoints();

app.Run();
