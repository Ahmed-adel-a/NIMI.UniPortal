namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Domain.Registration;

/// <summary>
/// Everything the registration rules need to judge one candidate group for one student (P4, §6a). Offerings build it
/// for every group to list all <c>reasons</c>; the register command builds it once and throws the first reason.
/// </summary>
public sealed record RegistrationContext(
    DateTimeOffset Now,
    RegistrationWindow? Window,
    bool GroupIsOffered,
    bool AlreadyRegisteredForSubject,
    bool IsMilitaryEducation,
    Guid? TargetDepartmentId,
    Guid? StudentDepartmentId,
    IReadOnlyList<string> MissingPrerequisiteCodes,
    int CurrentCredits,
    int SubjectCredits,
    IReadOnlyList<string> ClashingGroups);
