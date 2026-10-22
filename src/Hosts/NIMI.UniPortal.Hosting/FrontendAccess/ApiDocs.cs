using Scalar.AspNetCore;

namespace NIMI.UniPortal.Hosting.FrontendAccess;

/// <summary>
/// The OpenAPI document at <c>/openapi/v1.json</c> (what the frontend generates its client from) and a browsable page
/// at <c>/docs</c>. On in Development; elsewhere only with <c>ApiDocs:Enabled</c> = true.
/// </summary>
internal static class ApiDocs
{
    public static void AddApiDocs(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
    }

    public static void MapApiDocs(this WebApplication app)
    {
        var enabled = app.Environment.IsDevelopment()
            || string.Equals(app.Configuration["ApiDocs:Enabled"], "true", StringComparison.OrdinalIgnoreCase);
        if (!enabled)
            return;

        app.MapOpenApi();
        app.MapScalarApiReference("/docs", options => options
            .WithTitle("NIMI UniPortal API")
            .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.SchemeName));

        // The bare address (e.g. http://localhost:8080) opens the docs.
        app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();

        app.Lifetime.ApplicationStarted.Register(() =>
            app.Logger.LogInformation("API docs (Scalar): /docs, e.g. http://localhost:8080/docs with the frontend-dev compose file."));
    }
}
