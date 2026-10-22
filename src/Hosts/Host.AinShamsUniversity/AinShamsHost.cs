using NIMI.UniPortal.Hosting;
using NIMI.UniPortal.Modules.Library.Api;

namespace Host.AinShamsUniversity;

/// <summary>
/// The reference deployment: the core platform (both academic systems; its departments default to year groups) + Library. An explicit, namespaced entry point
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

        // ── Premium modules this deployment sells (P11): Library — not Forum or Events ──
        builder.Services.AddLibraryModule();

        var app = builder.Build();

        await app.UsePortalCoreAsync();
        app.MapLibraryEndpoints();

        await app.RunAsync();
    }
}
