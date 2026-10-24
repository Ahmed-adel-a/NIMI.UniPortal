namespace NIMI.UniPortal.Modules.StudentReports.Application.Common;

/// <summary>Who is asking: their id and the roles that matter for reports.</summary>
public sealed record Viewer(Guid UserId, bool IsAdmin, bool IsDepartmentHead, bool IsStudent);
