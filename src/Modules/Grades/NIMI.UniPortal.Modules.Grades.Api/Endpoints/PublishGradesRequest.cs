namespace NIMI.UniPortal.Modules.Grades.Api.Endpoints;

/// <summary>POST /api/admin/grades/publish: the term is required, subject and group narrow it.</summary>
public sealed record PublishGradesRequest(Guid? TermId, Guid? SubjectId, Guid? GroupId);
