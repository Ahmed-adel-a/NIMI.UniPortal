using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Library.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class LibraryDesignTimeDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>
{
    public LibraryDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<LibraryDbContext>)LibraryInfrastructure
            .Configure(new DbContextOptionsBuilder<LibraryDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
