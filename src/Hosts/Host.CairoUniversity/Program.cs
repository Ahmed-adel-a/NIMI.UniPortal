using System.Text.Json;
using System.Text.Json.Serialization;
using Host.CairoUniversity;
using Host.CairoUniversity.Errors;
using Host.CairoUniversity.Health;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NIMI.UniPortal.Modules.Platform.Api;
using NIMI.UniPortal.Modules.Users.Api;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

var builder = WebApplication.CreateBuilder(args);

// ── Cross-cutting ──────────────────────────────────────────────
builder.Services.AddSingleton(TimeProvider.System);                       // the only clock (DateTime.Now is banned)
builder.Services.AddMessaging();                                          // before modules: validation runs outermost
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();   // required by UseExceptionHandler(); our handler writes the actual envelope
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);   // malformed bodies → our envelope
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));

// JWT bearer: issuer/audience/keys come from configuration (Authentication:Schemes:Bearer).
// Real tokens are issued by POST /api/auth/login from P1.1; until then use `dotnet user-jwts` (see http/platform.http).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PortalPolicies.Admin, policy => policy.RequireRole(PortalRoles.Admin));

// ── Core modules ───────────────────────────────────────────────
builder.Services.AddPlatformModule();
builder.Services.AddUsersModule();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: [DatabaseHealthCheck.Tag], timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

await app.ApplyModuleMigrationsAsync();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// Process alive: runs no checks.
app.MapHealthChecks("/api/health", new HealthCheckOptions { Predicate = _ => false });
// Database reachable: 200 Healthy / 503 Unhealthy.
app.MapHealthChecks("/api/health/db", new HealthCheckOptions { Predicate = c => c.Tags.Contains(DatabaseHealthCheck.Tag) });

app.MapPlatformEndpoints();
app.MapUsersEndpoints();

app.Run();
