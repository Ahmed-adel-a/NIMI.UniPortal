using NIMI.UniPortal.Modules.Grades.Application.Drafting;

namespace NIMI.UniPortal.Modules.Grades.Api.Endpoints;

/// <summary>PUT /api/grades/groups/{groupId}: <c>{ entries: [{ studentId, score }] }</c>.</summary>
public sealed record DraftGradesRequest(IReadOnlyList<GradeEntry>? Entries);
