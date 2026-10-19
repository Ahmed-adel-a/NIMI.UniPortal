using Npgsql;

namespace NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

/// <summary>
/// A throwaway PostgreSQL database for one test run (decision TESTDB — no Docker).
/// The server comes from the PORTAL_TEST_DB environment variable: the local install on a dev
/// machine, the runner's PostgreSQL on CI. Created on start, dropped (WITH FORCE) at the end.
/// </summary>
public sealed class TestDatabase
{
    public const string EnvironmentVariable = "PORTAL_TEST_DB";

    private readonly NpgsqlConnectionStringBuilder _server;

    public TestDatabase()
    {
        var serverConnectionString = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(serverConnectionString))
            throw new InvalidOperationException(
                $"Integration tests need a PostgreSQL server. Set the {EnvironmentVariable} environment variable, " +
                "e.g. Host=localhost;Port=5432;Username=postgres;Password=<password> (no database name — each run creates its own).");

        _server = new NpgsqlConnectionStringBuilder(serverConnectionString) { Database = "postgres" };
        Name = $"portal_test_{Guid.CreateVersion7():N}";
        ConnectionString = new NpgsqlConnectionStringBuilder(_server.ConnectionString) { Database = Name }.ConnectionString;
    }

    public string Name { get; }

    public string ConnectionString { get; }

    public async Task CreateAsync()
    {
        await using var connection = new NpgsqlConnection(_server.ConnectionString);
        await connection.OpenAsync();
        // UTF-8 explicitly: Arabic data everywhere (trap #8). Template0 avoids inheriting a non-UTF-8 template1.
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{Name}\" ENCODING 'UTF8' TEMPLATE template0", connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DropAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(_server.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
}
