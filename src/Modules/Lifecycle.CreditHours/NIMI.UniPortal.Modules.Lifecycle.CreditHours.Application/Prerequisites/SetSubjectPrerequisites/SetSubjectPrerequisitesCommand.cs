using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Prerequisites.SetSubjectPrerequisites;

/// <summary>P4.2 — replaces a subject's whole prerequisite list (decision PREREQ).</summary>
public sealed record SetSubjectPrerequisitesCommand(
    Guid SubjectId, IReadOnlyList<Guid> PrerequisiteSubjectIds) : ICommand<SubjectPrerequisitesDto>;
