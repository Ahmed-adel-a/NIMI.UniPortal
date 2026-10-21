using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.AssignMilitaryEducation;

/// <summary>P5.5 — Student Affairs assigns students to a military-education group (any policy mode).</summary>
public sealed record AssignMilitaryEducationCommand(
    IReadOnlyList<Guid> StudentIds,
    Guid SubjectGroupId) : ICommand<MilitaryEducationAssignmentDto>;
