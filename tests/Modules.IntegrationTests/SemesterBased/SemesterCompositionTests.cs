// ACCEPTANCE TEST — written by the consultant for phase P4.B. Do not modify; report problems instead.
using System.Net;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;

namespace NIMI.UniPortal.Modules.IntegrationTests.SemesterBased;

/// <summary>P4.10: the reference host composes the SemesterBased variant instead of CreditHours (decision LIFECYCLE).</summary>
public sealed class SemesterCompositionTests(ReferenceHostFactory factory) : ReferenceHostTest(factory)
{
    [Fact]
    public async Task The_reference_host_maps_the_semester_routes_and_not_self_registration()
    {
        var admin = Factory.CreateClientAs("admin");
        var student = Factory.CreateClientAs("student");

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/promotion-policy")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync("/api/registration/offerings")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/admin/registration-windows")).StatusCode);
    }

    [Fact]
    public async Task The_shared_student_routes_answer_like_credit_hours()
    {
        var student = Factory.CreateClientAs("student");

        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync("/api/registration/progress")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync("/api/schedule?from=2027-02-06&to=2027-02-12")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync("/api/standing")).StatusCode);
    }
}
