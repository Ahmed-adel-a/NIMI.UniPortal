using Microsoft.EntityFrameworkCore;
using NIMI.UniPortal.Modules.Materials.Application.Abstractions;
using NIMI.UniPortal.Modules.Materials.Domain;

namespace NIMI.UniPortal.Modules.Materials.Infrastructure.Persistence;

/// <summary>The Materials module's own schema (P7, schema <c>materials</c>) — ids of other modules only, no cross-schema FKs.</summary>
public sealed class MaterialsDbContext(DbContextOptions<MaterialsDbContext> options) : DbContext(options), IMaterialsDbContext
{
    public const string Schema = "materials";

    public DbSet<Material> Materials => Set<Material>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaterialsDbContext).Assembly);
    }
}
