// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Library;

/// <summary>P11 — the reference deployment composes the core + Library, and not the premium modules Cairo bought.</summary>
public sealed class ReferenceHostCompositionTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task The_reference_host_maps_the_core_and_the_library()
    {
        Assert.Equal(HttpStatusCode.OK, (await Factory.CreateClient().GetAsync("/api/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Factory.CreateClientAs("student").GetAsync("/api/library/books")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Factory.CreateClientAs("admin").GetAsync("/api/admin/settings")).StatusCode);
    }

    [Fact]
    public async Task The_reference_host_does_not_map_forum_or_events()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Factory.CreateClientAs("student").GetAsync("/api/forum/categories")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Factory.CreateClientAs("student").GetAsync("/api/events")).StatusCode);
    }
}
