// ACCEPTANCE TEST — written by the consultant for phase P08. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Assignments;

/// <summary>Assignment and submission helpers through the real API (P8).</summary>
internal static class AssignmentsSetup
{
    public static readonly byte[] ReportBytes = Encoding.ASCII.GetBytes("%PDF-1.7\n% lab report\n%%EOF\n");

    public static async Task<HttpResponseMessage> CreateAsync(
        HttpClient teacher, Guid subjectId, DateTimeOffset dueAt, string title = "Lab 2 — Dijkstra", Guid? subjectGroupId = null,
        decimal maxGrade = 10m, string? allowedExtensions = null, int? maxFileMb = null, string? instructions = "Implement and test.") =>
        await teacher.PostAsJsonAsync("/api/assignments", new
        {
            subjectId, subjectGroupId, title, instructions, dueAt, maxGrade, allowedExtensions, maxFileMb,
        });

    public static async Task<Guid> CreatedAsync(
        HttpClient teacher, Guid subjectId, DateTimeOffset dueAt, string title = "Lab 2 — Dijkstra", Guid? subjectGroupId = null, decimal maxGrade = 10m)
    {
        var response = await CreateAsync(teacher, subjectId, dueAt, title, subjectGroupId, maxGrade);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    public static async Task<HttpResponseMessage> SubmitAsync(
        HttpClient student, Guid assignmentId, string fileName = "lab2.pdf", byte[]? content = null, string contentType = "application/pdf")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content ?? ReportBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return await student.PostAsync($"/api/assignments/{assignmentId}/submissions", form);
    }

    public static async Task<Guid> SubmittedAsync(HttpClient student, Guid assignmentId, string fileName = "lab2.pdf")
    {
        var response = await SubmitAsync(student, assignmentId, fileName);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }

    public static async Task<HttpResponseMessage> ReviewAsync(
        HttpClient teacher, Guid submissionId, string decision, decimal? grade = null, string? feedback = null, string? rejectionReason = null) =>
        await teacher.PutAsJsonAsync($"/api/submissions/{submissionId}/review", new { decision, grade, feedback, rejectionReason });
}
