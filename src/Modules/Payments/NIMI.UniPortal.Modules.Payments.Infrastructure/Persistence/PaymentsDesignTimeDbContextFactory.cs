using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NIMI.UniPortal.Modules.Payments.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef migrations add` build the model without the host. It never connects.</summary>
internal sealed class PaymentsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args) =>
        new((DbContextOptions<PaymentsDbContext>)PaymentsInfrastructure
            .Configure(new DbContextOptionsBuilder<PaymentsDbContext>(), "Host=localhost;Database=design_time")
            .Options);
}
