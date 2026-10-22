using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Infrastructure.Persistence.Configurations;

/// <summary>Maps the D4 identity snapshot onto <c>author_*</c> columns of threads and posts.</summary>
internal static class ForumAuthorColumns
{
    public static void Map<TOwner>(OwnedNavigationBuilder<TOwner, ForumAuthor> author)
        where TOwner : class
    {
        author.Property(a => a.Id).HasColumnName("author_id");
        author.Property(a => a.Name).HasColumnName("author_name").HasMaxLength(255);
        author.Property(a => a.Rank).HasColumnName("author_rank").HasMaxLength(50);
        author.Property(a => a.Department).HasColumnName("author_department").HasMaxLength(255);
        author.Property(a => a.DepartmentAr).HasColumnName("author_department_ar").HasMaxLength(255);
        author.HasIndex(a => a.Id);
    }
}
