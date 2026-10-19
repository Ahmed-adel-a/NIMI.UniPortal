using Microsoft.Extensions.Diagnostics.HealthChecks;
using NIMI.UniPortal.SharedKernel.Persistence;
using Npgsql;

namespace Host.CairoUniversity.Health;

/// <summary>Healthy when the deployment's PostgreSQL database answers a trivial query.</summary>
internal sealed class DatabaseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public const string Tag = "db";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString(PortalDatabase.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            return HealthCheckResult.Unhealthy($"Connection string '{PortalDatabase.ConnectionStringName}' is not configured.");

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            // Details go to the health-check log only; the HTTP response carries just the status.
            return HealthCheckResult.Unhealthy("Database unreachable.", ex);
        }
    }
}
