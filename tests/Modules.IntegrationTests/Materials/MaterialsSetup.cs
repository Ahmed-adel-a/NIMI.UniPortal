// ACCEPTANCE TEST — written by the consultant for phase P07-materials. Do not modify; report problems instead.
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using static NIMI.UniPortal.Modules.IntegrationTests.Scheduling.SchedulingSetup;

namespace NIMI.UniPortal.Modules.IntegrationTests.Materials;

/// <summary>Multipart upload helper for course materials (P7). Later phases (assignments) reuse the upload pattern.</summary>
internal static class MaterialsSetup
{
    public static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes("%PDF-1.7\n% NIMI test document\n%%EOF\n");

    public static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        Guid subjectId,
        string fileName = "graphs.pdf",
        byte[]? content = null,
        string title = "Lecture 3 — Graphs",
        Guid? subjectGroupId = null,
        int? weekNumber = null,
        string? description = null,
        string contentType = "application/pdf")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content ?? PdfBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        form.Add(new StringContent(subjectId.ToString()), "subjectId");
        form.Add(new StringContent(title), "title");
        if (subjectGroupId is { } group)
            form.Add(new StringContent(group.ToString()), "subjectGroupId");
        if (weekNumber is { } week)
            form.Add(new StringContent(week.ToString(System.Globalization.CultureInfo.InvariantCulture)), "weekNumber");
        if (description is not null)
            form.Add(new StringContent(description), "description");
        return await client.PostAsync("/api/materials", form);
    }

    public static async Task<Guid> UploadedAsync(
        HttpClient client, Guid subjectId, string fileName = "graphs.pdf", Guid? subjectGroupId = null, int? weekNumber = null,
        string title = "Lecture 3 — Graphs")
    {
        var response = await UploadAsync(client, subjectId, fileName, title: title, subjectGroupId: subjectGroupId, weekNumber: weekNumber);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (Guid)(await BodyOf(response))["id"]!;
    }
}
