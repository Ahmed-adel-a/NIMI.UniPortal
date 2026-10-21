using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites.GetSubjectPrerequisites;

/// <summary>P4.2 — the prerequisites of one subject, by code.</summary>
public sealed record GetSubjectPrerequisitesQuery(Guid SubjectId) : IQuery<SubjectPrerequisitesDto>;
