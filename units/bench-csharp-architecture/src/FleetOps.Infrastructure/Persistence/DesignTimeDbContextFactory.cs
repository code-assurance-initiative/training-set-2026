using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FleetOps.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef migrations add</c> build the context without starting a host.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FleetOpsDbContext>
{
    public FleetOpsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<FleetOpsDbContext>().UseSqlite("Data Source=fleetops.db").Options);
}
