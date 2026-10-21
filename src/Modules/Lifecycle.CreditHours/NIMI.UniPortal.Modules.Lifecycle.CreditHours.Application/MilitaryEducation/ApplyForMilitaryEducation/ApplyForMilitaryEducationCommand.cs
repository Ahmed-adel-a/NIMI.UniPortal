using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.MilitaryEducation.ApplyForMilitaryEducation;

/// <summary>P5.5 — a student applies for the military-education group in the active term (opt-in mode only).</summary>
public sealed record ApplyForMilitaryEducationCommand(
    Guid StudentId,
    bool IsStudent,
    Guid SubjectGroupId) : ICommand<MilitaryEducationApplicationDto>;
