using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rentals.Lending.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef migrations</c> build the context without the worker's host.</summary>
internal sealed class DesignTimeLendingDbContextFactory : IDesignTimeDbContextFactory<LendingDbContext>
{
    public LendingDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<LendingDbContext>().UseSqlite("Data Source=lending.db").Options);
}
