namespace NIMI.UniPortal.Modules.Forum.Domain;

/// <summary>
/// Decision D4 — who wrote a thread or post, snapshotted when it is written: name, rank (<c>student</c>, the academic
/// title of teaching staff, or <c>student_affairs</c>) and department. There is no anonymous author.
/// </summary>
public sealed record ForumAuthor(Guid Id, string Name, string Rank, string Department, string DepartmentAr);
