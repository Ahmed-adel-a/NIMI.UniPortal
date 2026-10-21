using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog;

/// <summary>A group as the HTTP API shows it; <c>registeredCount</c> stays 0 until registration (P4).</summary>
public sealed record GroupDto(
    Guid Id,
    Guid SubjectId,
    string SubjectCode,
    Guid TermId,
    int GroupNumber,
    Guid? TargetDepartmentId,
    int Capacity,
    bool IsActive,
    int RegisteredCount)
{
    public static GroupDto From(SubjectGroup group, string subjectCode) =>
        new(
            group.Id,
            group.SubjectId,
            subjectCode,
            group.TermId,
            group.GroupNumber,
            group.TargetDepartmentId,
            group.Capacity,
            group.IsActive,
            0);
}
