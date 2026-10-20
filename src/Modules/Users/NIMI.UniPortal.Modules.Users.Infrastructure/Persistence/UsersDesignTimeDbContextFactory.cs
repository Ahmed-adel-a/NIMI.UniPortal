using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Users.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class UsersDesignTimeDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<UsersDbContext>)UsersInfrastructure
            .Configure(new DbContextOptionsBuilder<UsersDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
