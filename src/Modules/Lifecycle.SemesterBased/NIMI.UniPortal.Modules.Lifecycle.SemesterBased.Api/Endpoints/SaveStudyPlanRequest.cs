using NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Application.Plans;

namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

public sealed record SaveStudyPlanRequest(int Levels, IReadOnlyList<StudyPlanEntry>? Subjects);
