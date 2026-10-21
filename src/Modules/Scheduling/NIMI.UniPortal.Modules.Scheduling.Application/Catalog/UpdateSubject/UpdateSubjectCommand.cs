using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.UpdateSubject;

public sealed record UpdateSubjectCommand(
    Guid Id,
    string Name,
    string NameAr,
    string? Description,
    int CreditHours,
    SubjectCategory Category,
    bool IsGraduationRequirement,
    bool IsActive) : ICommand<SubjectDto>;
