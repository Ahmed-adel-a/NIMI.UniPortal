// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Forum;

/// <summary>P11 — Cairo buys Forum (and Events) but not Library: only the bought premium modules are mapped.</summary>
public sealed class HostCompositionTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Cairo_maps_the_forum()
    {
        var response = await Factory.CreateClientAs("student").GetAsync("/api/forum/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cairo_does_not_map_the_library()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Factory.CreateClientAs("student").GetAsync("/api/library/books")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Factory.CreateClientAs("admin").GetAsync("/api/admin/library/loans")).StatusCode);
    }
}
