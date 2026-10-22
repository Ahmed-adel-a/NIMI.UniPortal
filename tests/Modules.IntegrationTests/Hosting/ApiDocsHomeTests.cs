// ACCEPTANCE TEST — written by the consultant for frontend enablement (decision DEVDOCKER). Do not modify; report problems instead.
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Hosting;

/// <summary>Where the API docs are on (Development, ApiDocs:Enabled), the bare address opens them.</summary>
[Collection(PortalCollection.Name)]
public sealed class ApiDocsHomeTests(PortalWebAppFactory factory)
{
    [Fact]
    public async Task The_root_address_redirects_to_the_docs_page()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/docs", response.Headers.Location?.OriginalString);
    }
}
