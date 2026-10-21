using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Grades.Application.Common;
using NIMI.UniPortal.Modules.Grades.Application.Drafting;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Grades.Api.Endpoints;

/// <summary>P9.1: a group's roster × grades (teaching staff, Student Affairs) and the teacher's drafts.</summary>
internal static class GroupGradeEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/grades/groups/{groupId:guid}")
            .WithTags("Grades")
            .RequireAuthorization();
        group.MapGet("", Get).WithName("GetGroupGrades");
        group.MapPut("", Draft).WithName("DraftGroupGrades");
    }

    private static async Task<Ok<GroupGradesDto>> Get(
        Guid groupId, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetGroupGradesQuery(groupId, CallerViewer.From(caller)), cancellationToken));

    private static async Task<Ok<GroupGradesDto>> Draft(
        Guid groupId, DraftGradesRequest request, ClaimsPrincipal caller, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new DraftGradesCommand(groupId, request.Entries, CallerViewer.From(caller)), cancellationToken));
}
