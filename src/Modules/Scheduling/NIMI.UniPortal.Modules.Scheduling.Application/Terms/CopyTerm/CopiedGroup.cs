using NIMI.UniPortal.Modules.Scheduling.Domain.Catalog;

namespace NIMI.UniPortal.Modules.Scheduling.Application.Terms.CopyTerm;

/// <summary>A group created in the new term, with what the slot copier needs to know about its subject.</summary>
internal sealed record CopiedGroup(SubjectGroup Group, string SubjectCode, Guid DepartmentId);
