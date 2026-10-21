using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;

/// <summary>P9.4 GRADREQ: Student Affairs sets (or changes) a department's credits to graduate.</summary>
public sealed record SetGraduationRequirementCommand(Guid DepartmentId, int RequiredCredits, Guid AdminId)
    : ICommand<GraduationRequirementDto>;
