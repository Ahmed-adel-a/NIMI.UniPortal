using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using NIMI.UniPortal.SharedKernel;
using NIMI.UniPortal.SharedKernel.Documents;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Hosting.Documents;

/// <summary>
/// Decision PDFDOCS: printable PDFs. Students get their own documents; Student Affairs gets any student's
/// transcript and certificate, and any receipt.
/// </summary>
internal static class DocumentEndpoints
{
    public static IServiceCollection AddDocuments(this IServiceCollection services) =>
        services.AddScoped<DocumentComposer>();

    public static void MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var mine = endpoints.MapGroup("/api/documents").WithTags("Documents").RequireAuthorization();
        mine.MapGet("/receipts/{paymentId:guid}", Receipt).WithName("GetReceiptPdf");
        mine.MapGet("/transcript", MyTranscript).WithName("GetMyTranscriptPdf");
        mine.MapGet("/enrollment-certificate", MyCertificate).WithName("GetMyEnrollmentCertificatePdf");
        mine.MapGet("/schedule", MySchedule).WithName("GetMySchedulePdf");

        var admin = endpoints.MapGroup("/api/admin/documents/students/{studentId:guid}")
            .WithTags("Documents")
            .RequireAuthorization(PortalPolicies.Admin);
        admin.MapGet("/transcript", StudentTranscript).WithName("GetStudentTranscriptPdf");
        admin.MapGet("/enrollment-certificate", StudentCertificate).WithName("GetStudentEnrollmentCertificatePdf");
    }

    private static async Task<FileContentHttpResult> Receipt(
        Guid paymentId, ClaimsPrincipal caller, DocumentComposer documents, CancellationToken cancellationToken) =>
        Pdf(await documents.ReceiptAsync(paymentId, caller.GetUserId(), caller.IsInRole(PortalRoles.Admin), cancellationToken),
            $"receipt-{paymentId:N}.pdf");

    private static async Task<FileContentHttpResult> MyTranscript(
        ClaimsPrincipal caller, DocumentComposer documents, CancellationToken cancellationToken) =>
        Pdf(await documents.TranscriptAsync(StudentOnly(caller), cancellationToken), "grade-report.pdf");

    private static async Task<FileContentHttpResult> MyCertificate(
        ClaimsPrincipal caller, DocumentComposer documents, CancellationToken cancellationToken) =>
        Pdf(await documents.CertificateAsync(StudentOnly(caller), cancellationToken), "enrollment-certificate.pdf");

    private static async Task<FileContentHttpResult> MySchedule(
        DateOnly? from, DateOnly? to, ClaimsPrincipal caller, DocumentComposer documents, CancellationToken cancellationToken) =>
        Pdf(await documents.ScheduleAsync(StudentOnly(caller), from, to, cancellationToken),
            string.Create(CultureInfo.InvariantCulture, $"schedule-{from:yyyy-MM-dd}-{to:yyyy-MM-dd}.pdf"));

    private static async Task<FileContentHttpResult> StudentTranscript(
        Guid studentId, DocumentComposer documents, CancellationToken cancellationToken) =>
        Pdf(await documents.TranscriptAsync(studentId, cancellationToken), "grade-report.pdf");

    private static async Task<FileContentHttpResult> StudentCertificate(
        Guid studentId, DocumentComposer documents, CancellationToken cancellationToken) =>
        Pdf(await documents.CertificateAsync(studentId, cancellationToken), "enrollment-certificate.pdf");

    private static Guid StudentOnly(ClaimsPrincipal caller) =>
        caller.IsInRole(PortalRoles.Student)
            ? caller.GetUserId()
            : throw new ForbiddenException("These documents are for students; Student Affairs uses /api/admin/documents.");

    private static FileContentHttpResult Pdf(byte[] content, string fileName) =>
        TypedResults.File(content, PrintedDocument.ContentType, fileName);
}
