using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Grades.Api.Csv;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.Modules.Grades.Application.Drafting;
using NIMI.UniPortal.Modules.Grades.Application.Importing;
using NIMI.UniPortal.SharedKernel.Messaging;
using CsvFile = NIMI.UniPortal.SharedKernel.Text.Csv;

namespace NIMI.UniPortal.Modules.Grades.Api.Endpoints;

/// <summary>
/// Grades CSV import (decision BACKENDDONE): the group's roster as a CSV template (student code, name, current
/// score), and the filled-in file uploaded back as drafts, all or nothing.
/// </summary>
internal static class GradeCsvEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/grades/groups/{groupId:guid}")
            .WithTags("Grades")
            .RequireAuthorization();
        group.MapGet("/template", Template).WithName("GetGradesTemplate");

        // Bearer-token API (no cookies), so the browser-form antiforgery check doesn't apply.
        group.MapPost("/import", Import).DisableAntiforgery().WithName("ImportGroupGrades");
    }

    private static async Task<FileContentHttpResult> Template(
        Guid groupId, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        var grades = await sender.Send(new GetGroupGradesQuery(groupId, CallerViewer.From(caller)), cancellationToken);

        var csv = new CsvFile().Row("student_code", "full_name", "score");
        foreach (var row in grades.Rows)
            csv.Row(row.StudentCode ?? string.Empty, row.FullName, row.Score?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty);

        var fileName = string.Create(CultureInfo.InvariantCulture, $"grades-{grades.SubjectCode}-group-{grades.GroupNumber}.csv");
        return TypedResults.File(csv.ToUtf8WithBom(), CsvFile.ContentType, fileName);
    }

    private static async Task<Ok<GroupGradesDto>> Import(
        Guid groupId, IFormFile? file, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw GradeCsvReader.FileError("Attach the CSV in the form field 'file'.");
        if (file.Length > GradeCsvReader.MaxBytes)
            throw GradeCsvReader.FileError("The file is larger than 2 MB.");

        await using var stream = file.OpenReadStream();
        var rows = await GradeCsvReader.ReadAsync(stream, cancellationToken);
        return TypedResults.Ok(await sender.Send(new ImportGradesCommand(groupId, rows, CallerViewer.From(caller)), cancellationToken));
    }
}
