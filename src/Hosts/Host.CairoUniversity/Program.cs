using Host.CairoUniversity.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: [DatabaseHealthCheck.Tag], timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

// Process alive: runs no checks.
app.MapHealthChecks("/api/health", new HealthCheckOptions { Predicate = _ => false });
// Database reachable: 200 Healthy / 503 Unhealthy.
app.MapHealthChecks("/api/health/db", new HealthCheckOptions { Predicate = c => c.Tags.Contains(DatabaseHealthCheck.Tag) });

app.Run();
