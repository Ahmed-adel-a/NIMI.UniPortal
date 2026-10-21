namespace NIMI.UniPortal.Modules.Grades.Contracts;

/// <summary>Drafts waiting for confirmation and confirmed grades waiting for publication (decision D3).</summary>
public sealed record GradeBacklog(int AwaitingConfirmation, int AwaitingPublication);
