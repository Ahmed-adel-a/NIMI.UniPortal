// ACCEPTANCE TEST — written by the consultant for PDF exports (decision PDFDOCS). Do not modify; report problems instead.
using System.Net;
using System.Text;
using NIMI.UniPortal.Modules.IntegrationTests.Infrastructure;
using static NIMI.UniPortal.Modules.IntegrationTests.Grades.GradesSetup;
using static NIMI.UniPortal.Modules.IntegrationTests.Payments.PaymentsSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Hosting;

/// <summary>
/// Decision PDFDOCS: English PDFs: payment receipt, grade report, enrollment certificate and class schedule.
/// Students get their own; Student Affairs gets any student's transcript and certificate.
/// </summary>
public sealed class DocumentTests(PortalWebAppFactory factory) : PortalTest(factory)
{
    [Fact]
    public async Task A_student_downloads_their_own_receipt_and_nobody_elses()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await GradedCampusAsync(Factory, admin);
        var fee = await CreateFeeItemAsync(admin, "Tuition", "tuition", 6500m, w.TermId);
        var payment = await RecordedAsync(admin, w.Ahmed, fee, 6500m);

        var response = await w.AhmedClient.GetAsync($"/api/documents/receipts/{payment["id"]}");

        var pdf = await PdfAsync(response);
        Assert.Contains($"({payment["receiptNumber"]}) Tj", pdf);
        Assert.Contains("(6,500.00) Tj", pdf);
        Assert.Contains("(Payment receipt) Tj", pdf);
        Assert.NotEqual(HttpStatusCode.OK, (await w.MonaClient.GetAsync($"/api/documents/receipts/{payment["id"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/documents/receipts/{payment["id"]}")).StatusCode);
    }

    [Fact]
    public async Task The_grade_report_lists_published_grades_for_the_student_and_student_affairs()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await GradedCampusAsync(Factory, admin);
        await GradeAndPublishAsync(w.HanyClient, admin, w.TermId, w.Cs301Group1, (w.Ahmed, 91m), (w.Mona, 72m));

        var mine = await PdfAsync(await w.AhmedClient.GetAsync("/api/documents/transcript"));
        var theirs = await PdfAsync(await admin.GetAsync($"/api/admin/documents/students/{w.Ahmed}/transcript"));

        Assert.Contains("(Grade report) Tj", mine);
        Assert.Contains("(CS301) Tj", mine);
        Assert.Contains("(91) Tj", mine);
        Assert.Contains("(CS301) Tj", theirs);
    }

    [Fact]
    public async Task The_enrollment_certificate_is_for_students_and_student_affairs()
    {
        var admin = Factory.CreateClientAs("admin");
        var w = await GradedCampusAsync(Factory, admin);

        var pdf = await PdfAsync(await w.AhmedClient.GetAsync("/api/documents/enrollment-certificate"));

        Assert.Contains("(Enrollment certificate) Tj", pdf);
        Assert.Contains("This is to certify", pdf);
        Assert.Equal(HttpStatusCode.Forbidden, (await w.HanyClient.GetAsync("/api/documents/enrollment-certificate")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/admin/documents/students/{w.Mona}/enrollment-certificate")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await admin.GetAsync($"/api/admin/documents/students/{Guid.CreateVersion7()}/enrollment-certificate")).StatusCode);
    }

    [Fact]
    public async Task The_schedule_pdf_lists_the_classes_of_the_period()
    {
        var w = await GradedCampusAsync(Factory, Factory.CreateClientAs("admin"));

        var pdf = await PdfAsync(await w.AhmedClient.GetAsync("/api/documents/schedule?from=2026-10-03&to=2026-10-16"));

        Assert.Contains("(Class schedule) Tj", pdf);
        Assert.Contains("(CS301) Tj", pdf);
        Assert.Contains("(Sunday) Tj", pdf);
        Assert.Equal(HttpStatusCode.BadRequest, (await w.AhmedClient.GetAsync("/api/documents/schedule?to=2026-10-16")).StatusCode);
    }

    private static async Task<string> PdfAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var pdf = Encoding.Latin1.GetString(await response.Content.ReadAsByteArrayAsync());
        Assert.StartsWith("%PDF-1.4", pdf);
        return pdf;
    }
}
