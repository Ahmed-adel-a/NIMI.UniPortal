using NIMI.UniPortal.SharedKernel.Persistence;

namespace Host.CairoUniversity;

internal static class ModuleMigrations
{
    public const string ApplyOnStartupKey = "Database:ApplyMigrationsOnStartup";

    /// <summary>
    /// Applies every module's pending migrations (each module owns its schema + history table).
    /// Opt-in via configuration: on in Development and tests; production runs migrations as a deliberate deploy step.
    /// </summary>
    public static async Task ApplyModuleMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(ApplyOnStartupKey))
            return;

        await using var scope = app.Services.CreateAsyncScope();
        foreach (var database in scope.ServiceProvider.GetServices<IModuleDatabase>())
        {
            app.Logger.LogInformation("Applying migrations for module {Module}", database.Module);
            await database.MigrateAsync(app.Lifetime.ApplicationStopping);
        }
    }
}
