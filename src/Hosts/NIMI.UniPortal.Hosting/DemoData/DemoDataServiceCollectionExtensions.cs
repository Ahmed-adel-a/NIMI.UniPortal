using NIMI.UniPortal.SharedKernel.Persistence;

namespace NIMI.UniPortal.Hosting.DemoData;

internal static class DemoDataServiceCollectionExtensions
{
    /// <summary>After every core module, so the campus seeder runs after their own seeders (e.g. the first admin).</summary>
    public static IServiceCollection AddDemoData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(DemoDataOptions.From(configuration));
        services.AddScoped<DemoCampus>();
        services.AddScoped<IModuleSeeder, DemoCampusSeeder>();
        return services;
    }
}
