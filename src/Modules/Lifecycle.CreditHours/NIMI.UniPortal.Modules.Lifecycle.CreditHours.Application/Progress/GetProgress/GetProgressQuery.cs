using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.CreditHours.Application.Progress.GetProgress;

/// <summary>P4.6 — the signed-in student's own progress.</summary>
public sealed record GetProgressQuery(Guid StudentId, bool IsStudent) : IQuery<StudentProgressDto>;
