using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Graduation;

/// <summary>P9.4 GRADREQ: one department's requirement (unknown department → 404).</summary>
public sealed record GetGraduationRequirementQuery(Guid DepartmentId) : IQuery<GraduationRequirementDto>;
