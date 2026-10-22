// ACCEPTANCE TEST — written by the consultant for phase P12 prep (frontend enablement). Do not modify; report problems instead.
using System.Net;
using System.Text.Json.Nodes;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Hosting;

/// <summary>
/// What the React app (another origin) needs before it can call the API: CORS for the configured origins only
/// (<c>Cors:AllowedOrigins</c>; the test host allows http://localhost:5173), error envelopes and file names readable
/// cross-origin, and a machine-readable OpenAPI document with a browsable page (<c>ApiDocs:Enabled</c>).
/// </summary>
[Collection(PortalCollection.Name)]
public sealed class FrontendAccessTests(PortalWebAppFactory factory)
{
    private const string Frontend = "http://localhost:5173";

    [Fact]
    public async Task A_preflight_from_an_allowed_origin_is_answered()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", Frontend);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await factory.CreateClient().SendAsync(request);

        Assert.True(response.IsSuccessStatusCode, $"preflight answered {(int)response.StatusCode}");
        Assert.Equal(Frontend, Header(response, "Access-Control-Allow-Origin"));
        Assert.Contains("POST", Header(response, "Access-Control-Allow-Methods"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("authorization", Header(response, "Access-Control-Allow-Headers"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task An_unknown_origin_gets_no_cors_headers()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/settings");
        request.Headers.Add("Origin", "https://evil.example");

        var response = await factory.CreateClient().SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Error_envelopes_are_readable_cross_origin_and_file_names_are_exposed()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");   // no token → 401 envelope
        request.Headers.Add("Origin", Frontend);

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(Frontend, Header(response, "Access-Control-Allow-Origin"));
        Assert.Contains("Content-Disposition", Header(response, "Access-Control-Expose-Headers"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task The_openapi_document_describes_the_api_with_bearer_auth()
    {
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var paths = document["paths"]!.AsObject();
        Assert.Contains("/api/auth/login", paths.Select(p => p.Key));
        Assert.Contains("/api/dashboard", paths.Select(p => p.Key));
        Assert.Contains("/api/forum/categories", paths.Select(p => p.Key));   // premium modules of the host are included
        Assert.Equal("bearer", (string?)document["components"]!["securitySchemes"]!["Bearer"]!["scheme"]);
    }

    [Fact]
    public async Task The_docs_page_is_served()
    {
        var response = await factory.CreateClient().GetAsync("/docs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    private static string Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : "";
}
