namespace NIMI.UniPortal.Modules.Lifecycle.SemesterBased.Api.Endpoints;

public sealed record GenerateEnrollmentRequest(Guid TermId, Guid? DepartmentId);
