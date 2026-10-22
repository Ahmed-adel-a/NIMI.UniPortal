using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Application.Common;

/// <summary>The identity shown on every thread and post (D4).</summary>
public sealed record AuthorDto(Guid Id, string Name, string Rank, string Department, string DepartmentAr)
{
    public static AuthorDto From(ForumAuthor author) =>
        new(author.Id, author.Name, author.Rank, author.Department, author.DepartmentAr);
}
