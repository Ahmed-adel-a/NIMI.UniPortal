using Host.CairoUniversity;
using Host.CairoUniversity.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NIMI.UniPortal.Modules.Platform.Api;
using NIMI.UniPortal.SharedKernel.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessaging();

// ── Core modules ───────────────────────────────────────────────
builder.Services.AddPlatformModule();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: [DatabaseHealthCheck.Tag], timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

await app.ApplyModuleMigrationsAsync();

// Process alive: runs no checks.
app.MapHealthChecks("/api/health", new HealthCheckOptions { Predicate = _ => false });
// Database reachable: 200 Healthy / 503 Unhealthy.
app.MapHealthChecks("/api/health/db", new HealthCheckOptions { Predicate = c => c.Tags.Contains(DatabaseHealthCheck.Tag) });

app.MapPlatformEndpoints();

app.Run();
