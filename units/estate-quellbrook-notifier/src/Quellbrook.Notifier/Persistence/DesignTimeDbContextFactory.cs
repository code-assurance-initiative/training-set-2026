using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Quellbrook.Notifier.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> and by the migrations bundle. The connection string comes from the environment, never from
/// the command line, so it does not show up in process listings or CI logs.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NotifierDbContext>
{
    public NotifierDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Notifier")
            ?? "Host=localhost;Database=notifier;Username=notifier";
        return new NotifierDbContext(new DbContextOptionsBuilder<NotifierDbContext>().UseNpgsql(connectionString).Options);
    }
}
