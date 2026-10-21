namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>POST /api/admin/subjects/{subjectId}/groups</c> (P3.3).</summary>
public sealed record CreateGroupRequest(
    Guid TermId,
    int GroupNumber,
    Guid? TargetDepartmentId,
    int Capacity);
