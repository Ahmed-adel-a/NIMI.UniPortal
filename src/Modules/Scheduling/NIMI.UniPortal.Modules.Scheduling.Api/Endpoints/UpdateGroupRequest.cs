namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>PUT /api/admin/groups/{id}</c>.</summary>
public sealed record UpdateGroupRequest(
    int Capacity,
    Guid? TargetDepartmentId,
    bool IsActive);
