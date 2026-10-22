using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Infrastructure.Persistence.Configurations;

/// <summary>forum.posts — replies in posting order.</summary>
internal sealed class ForumPostConfiguration : IEntityTypeConfiguration<ForumPost>
{
    public void Configure(EntityTypeBuilder<ForumPost> builder)
    {
        builder.ToTable("posts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.HasOne<ForumThread>().WithMany().HasForeignKey(p => p.ThreadId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsOne(p => p.Author, ForumAuthorColumns.Map);
        builder.Navigation(p => p.Author).IsRequired();
        builder.Property(p => p.Body).HasMaxLength(ForumText.MaxBodyLength);
        builder.HasIndex(p => new { p.ThreadId, p.CreatedAt });
        builder.HasIndex(p => p.CreatedAt);
    }
}
