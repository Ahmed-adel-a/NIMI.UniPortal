namespace NIMI.UniPortal.Hosting.FrontendAccess;

/// <summary>
/// The React app runs on another origin. Only the origins in <c>Cors:AllowedOrigins</c> may call the API (none
/// configured = no cross-origin access at all). Tokens travel in the Authorization header, so no credentials mode;
/// <c>Content-Disposition</c> is exposed so downloads and exports keep their file names.
/// </summary>
internal static class FrontendCors
{
    private const string PolicyName = "frontend";

    public static void AddFrontendCors(this WebApplicationBuilder builder)
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        builder.Services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition")
            .SetPreflightMaxAge(TimeSpan.FromHours(1))));
    }

    public static void UseFrontendCors(this WebApplication app) => app.UseCors(PolicyName);
}
