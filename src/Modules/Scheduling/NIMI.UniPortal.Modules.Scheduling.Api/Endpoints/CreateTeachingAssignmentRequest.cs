using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Api.Endpoints;

/// <summary>Body of <c>POST /api/admin/teaching-assignments</c> (P3.3).</summary>
public sealed record CreateTeachingAssignmentRequest(
    Guid StaffId,
    Guid SubjectId,
    Guid TermId,
    Guid? SubjectGroupId,
    TeachingRole Role);
