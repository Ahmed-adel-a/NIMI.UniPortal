using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Forum.Application.Abstractions;
using NIMI.UniPortal.Modules.Forum.Domain;

namespace NIMI.UniPortal.Modules.Forum.Infrastructure.Persistence;

/// <summary>The Forum premium module's own schema (P11, schema <c>forum</c>) — ids of other modules only.</summary>
public sealed class ForumDbContext(DbContextOptions<ForumDbContext> options)
    : DbContext(options), IForumDbContext
{
    public const string Schema = "forum";

    public DbSet<ForumCategory> Categories => Set<ForumCategory>();

    public DbSet<ForumThread> Threads => Set<ForumThread>();

    public DbSet<ForumPost> Posts => Set<ForumPost>();

    public DbSet<ForumReport> Reports => Set<ForumReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ForumDbContext).Assembly);
    }
}
