namespace NIMI.UniPortal.Modules.Library.Application.Common;

/// <summary>Who is asking: their id and which of the three roles they hold.</summary>
public sealed record Viewer(Guid UserId, bool IsAdmin, bool IsProfessor, bool IsStudent);
