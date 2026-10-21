using NIMI.UniPortal.Abstractions.Files;

namespace Host.CairoUniversity.Storage;

/// <summary>Registers the deployment's file storage (local disk under <c>Storage:RootPath</c>).</summary>
internal static class FileStorageServiceCollectionExtensions
{
    public const string RootPathKey = "Storage:RootPath";

    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration[RootPathKey];
        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "files")
            : Path.GetFullPath(configured);

        services.AddSingleton<IFileStorage>(provider => new LocalFileStorage(root, provider.GetRequiredService<TimeProvider>()));
        return services;
    }
}
