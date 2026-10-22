using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>
/// P4.7: Student Affairs places a student in a year level of their department (new students start in level 1
/// automatically when a term is generated; this is for transfers and corrections).
/// </summary>
public sealed record PlaceStudentCommand(Guid StudentId, int Level) : ICommand<StandingDto>;
