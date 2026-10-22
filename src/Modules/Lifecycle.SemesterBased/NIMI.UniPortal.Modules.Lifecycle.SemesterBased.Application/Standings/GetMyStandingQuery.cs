using NIMI.UniPortal.SharedKernel.Messaging;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Standings;

/// <summary>P4.9: the signed-in student's own standing (404 until they are placed in a level).</summary>
public sealed record GetMyStandingQuery(Guid StudentId, bool IsStudent) : IQuery<MyStandingDto>;
