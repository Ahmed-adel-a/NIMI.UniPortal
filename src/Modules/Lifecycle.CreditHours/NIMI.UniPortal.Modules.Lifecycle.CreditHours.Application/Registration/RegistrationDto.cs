using NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Registration;

/// <summary>The registration a successful <c>POST /api/registration</c> created (P4.3).</summary>
public sealed record RegistrationDto(
    Guid Id,
    Guid SubjectId,
    string SubjectCode,
    Guid GroupId,
    int GroupNumber,
    int CreditHours,
    RegistrationStatus Status,
    DateTimeOffset RegisteredAt);
