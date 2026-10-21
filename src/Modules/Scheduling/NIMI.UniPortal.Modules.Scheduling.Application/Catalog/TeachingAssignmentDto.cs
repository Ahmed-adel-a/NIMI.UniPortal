using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog;

/// <summary>Who teaches what, with the staff name resolved through <c>IUsersApi</c>.</summary>
public sealed record TeachingAssignmentDto(
    Guid Id,
    Guid StaffId,
    string StaffName,
    Guid SubjectId,
    string SubjectCode,
    Guid TermId,
    Guid? SubjectGroupId,
    int? GroupNumber,
    TeachingRole Role,
    bool IsActive);
