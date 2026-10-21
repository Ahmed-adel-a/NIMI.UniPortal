using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Grades.Application.Mine;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Api.Endpoints;

/// <summary>P9.3: the signed-in student's published grades and GPA.</summary>
internal static class MyGradesEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/grades/mine", Mine)
            .WithTags("Grades")
            .RequireAuthorization()
            .WithName("MyGrades");

    private static async Task<Ok<MyGradesDto>> Mine(
        Guid? termId, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new MyGradesQuery(termId, CallerViewer.From(caller)), cancellationToken));
}
