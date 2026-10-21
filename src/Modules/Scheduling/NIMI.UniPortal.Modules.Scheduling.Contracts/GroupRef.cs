namespace NIMI.UniPortal.Modules.Scheduling.Contracts;

/// <summary>A subject group (شعبة) of one term.</summary>
public sealed record GroupRef(
    Guid Id,
    Guid SubjectId,
    Guid TermId,
    int GroupNumber,
    Guid? TargetDepartmentId,
    int Capacity,
    bool IsActive);
