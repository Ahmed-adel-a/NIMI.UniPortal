using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Infrastructure.Persistence.Configurations;

/// <summary>forum.threads — author snapshot inline; listed by category, pinned first, latest activity.</summary>
internal sealed class ForumThreadConfiguration : IEntityTypeConfiguration<ForumThread>
{
    public void Configure(EntityTypeBuilder<ForumThread> builder)
    {
        builder.ToTable("threads", table => table.HasCheckConstraint("ck_threads_reply_count", "reply_count >= 0"));
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.HasOne<ForumCategory>().WithMany().HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsOne(t => t.Author, ForumAuthorColumns.Map);
        builder.Navigation(t => t.Author).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(ForumText.MaxTitleLength);
        builder.Property(t => t.Body).HasMaxLength(ForumText.MaxBodyLength);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(t => new { t.CategoryId, t.IsPinned, t.LastActivityAt });
        builder.HasIndex(t => t.CreatedAt);
    }
}
