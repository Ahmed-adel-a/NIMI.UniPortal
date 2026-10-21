using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Materials.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class MaterialsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MaterialsDbContext>
{
    public MaterialsDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<MaterialsDbContext>)MaterialsInfrastructure
            .Configure(new DbContextOptionsBuilder<MaterialsDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
