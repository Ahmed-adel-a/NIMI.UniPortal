// ACCEPTANCE TEST — written by the consultant for phase P11. Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.Events;

/// <summary>P11 — Cairo bought Events, so its routes are mapped.</summary>
public sealed class EventsHostCompositionTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task Cairo_maps_events()
    {
        Assert.Equal(HttpStatusCode.OK, (await Factory.CreateClientAs("student").GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Factory.CreateClientAs("admin").GetAsync("/api/admin/events")).StatusCode);
    }
}
