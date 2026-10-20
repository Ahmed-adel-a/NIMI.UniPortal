using NIMI.UniPortal.SharedKernel.Persistence;

namespace Host.CairoUniversity;

internal static class ModuleSeeding
{
    /// <summary>Development only: every module's starting data (e.g. the first admin from SEED_ADMIN_* settings).</summary>
    public static async Task RunDevelopmentSeedersAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        await using var scope = app.Services.CreateAsyncScope();
        foreach (var seeder in scope.ServiceProvider.GetServices<IModuleSeeder>())
            await seeder.SeedAsync(app.Lifetime.ApplicationStopping);
    }
}
