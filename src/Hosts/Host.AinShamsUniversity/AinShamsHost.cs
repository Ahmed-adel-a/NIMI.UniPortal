using NIMI.UniPortal.Hosting;
using NIMI.UniPortal.Modules.Library.Api;
using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Api;

namespace Host.AinShamsUniversity;

/// <summary>
/// The reference deployment: the core platform + one lifecycle variant + Library. An explicit, namespaced entry point
/// (no top-level statements) so its type never clashes with another host's public <c>Program</c> in the test project.
/// </summary>
public sealed class AinShamsHost
{
    private AinShamsHost()
    {
    }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddPortalCore();
        builder.Services.AddCreditHoursModule();   // the one lifecycle variant (SemesterBased once P4.B exists)

        // ── Premium modules this deployment sells (P11): Library — not Forum or Events ──
        builder.Services.AddLibraryModule();

        var app = builder.Build();

        await app.UsePortalCoreAsync();
        app.MapCreditHoursEndpoints();
        app.MapLibraryEndpoints();

        await app.RunAsync();
    }
}
