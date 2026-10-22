using NIMI.UniPortal.Modules.Users.Contracts;

namespace NIMI.UniPortal.Hosting.Documents;

/// <summary>Who a document is about: the account, its placement and its department and faculty (when known).</summary>
internal sealed record DocumentStudent(UserRef User, StudentPlacementRef? Placement, DepartmentRef? Department, FacultyRef? Faculty);
