using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration;

/// <summary>One of the student's live registrations in a term (P4.3).</summary>
public sealed record MyRegistrationDto(
    Guid Id,
    Guid SubjectId,
    string SubjectCode,
    string SubjectName,
    string SubjectNameAr,
    Guid GroupId,
    int GroupNumber,
    int CreditHours,
    RegistrationStatus Status,
    DateTimeOffset RegisteredAt);
