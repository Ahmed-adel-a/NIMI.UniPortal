using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Users.Application.Abstractions;
using NIMI.UniPortal.Modules.Users.Domain.Accounts;
using NIMI.UniPortal.Modules.Users.Domain.Organization;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options), IUsersDbContext
{
    public const string Schema = "users";

    public DbSet<Faculty> Faculties => Set<Faculty>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
