using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;
using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Catalog.CreateSubject;

public sealed record CreateSubjectCommand(
    Guid DepartmentId,
    string Code,
    string Name,
    string NameAr,
    string? Description,
    int CreditHours,
    SubjectCategory Category,
    bool IsGraduationRequirement) : ICommand<SubjectDto>;
