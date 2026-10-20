namespace NIMI.UniPortal.Modules.Users.Contracts;

/// <summary>A faculty as other modules see it.</summary>
public sealed record FacultyRef(Guid Id, string Code, string Name, string NameAr, bool IsActive);
