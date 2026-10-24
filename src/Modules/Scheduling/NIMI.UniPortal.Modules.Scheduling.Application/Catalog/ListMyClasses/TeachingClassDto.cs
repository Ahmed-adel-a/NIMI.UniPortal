namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.ListMyClasses;

/// <summary>One group the caller teaches this term, with the ids the teaching screens need.</summary>
public sealed record TeachingClassDto(
    Guid TermId,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string SubjectNameAr,
    int CreditHours,
    Guid GroupId,
    int GroupNumber);
