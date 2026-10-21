using NIMI.UniPortal.Modules.Assignments.Domain;

namespace NIMI.UniPortal.Modules.Assignments.Application.Common;

/// <summary>An assignment the caller may see, and whether the caller teaches its subject.</summary>
internal sealed record VisibleAssignment(Assignment Assignment, bool Teaches);
