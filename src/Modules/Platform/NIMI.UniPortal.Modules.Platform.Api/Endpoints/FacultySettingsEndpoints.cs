using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using NIMI.UniPortal.Modules.Platform.Application.Settings.DeleteFacultySettings;
using NIMI.UniPortal.Modules.Platform.Application.Settings.GetFacultySettings;
using NIMI.UniPortal.Modules.Platform.Application.Settings.UpdateFacultySettings;
using NIMI.UniPortal.SharedKernel.Messaging;
using NIMI.UniPortal.SharedKernel.Security;

namespace NIMI.UniPortal.Modules.Platform.Api.Endpoints;

/// <summary>Per-faculty overrides of the academic rules (P1.4). Student Affairs only (decision ADMINROUTE).</summary>
internal static class FacultySettingsEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/faculties/{facultyId:guid}/settings")
            .WithTags("Platform")
            .RequireAuthorization(PortalPolicies.Admin);

        admin.MapGet("/", GetFacultySettings).WithName("GetFacultySettings");
        admin.MapPut("/", UpdateFacultySettings).WithName("UpdateFacultySettings");
        admin.MapDelete("/", DeleteFacultySettings).WithName("DeleteFacultySettings");
    }

    private static async Task<Ok<FacultySettingsDto>> GetFacultySettings(
        Guid facultyId, ISender sender, CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new GetFacultySettingsQuery(facultyId), cancellationToken));

    /// <summary>Full replace. Returns the saved overrides + effective rules (re-read after the commit).</summary>
    private static async Task<Ok<FacultySettingsDto>> UpdateFacultySettings(
        Guid facultyId, FacultySettingsRequest request, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateFacultySettingsCommand(
                facultyId,
                request.WeekStartDay,
                request.WorkingDays,
                request.AttendanceThreshold,
                request.LateGraceMinutes,
                request.GradingScale,
                request.StudentCodeFormat,
                request.MilitaryEducationPolicy,
                request.Version),
            cancellationToken);

        return TypedResults.Ok(await sender.Send(new GetFacultySettingsQuery(facultyId), cancellationToken));
    }

    /// <summary>Deletes every override; 204 and idempotent.</summary>
    private static async Task<NoContent> DeleteFacultySettings(
        Guid facultyId, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteFacultySettingsCommand(facultyId), cancellationToken);
        return TypedResults.NoContent();
    }
}
