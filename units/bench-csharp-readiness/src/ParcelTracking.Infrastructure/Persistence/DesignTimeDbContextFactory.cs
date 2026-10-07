using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ParcelTracking.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> and by the migrations bundle. The connection string comes from the environment, never from
/// the command line, so it does not show up in process listings or CI logs.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrackingDbContext>
{
    public TrackingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Tracking")
            ?? "Host=localhost;Database=parcel_tracking;Username=parcel_tracking";
        var options = new DbContextOptionsBuilder<TrackingDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new TrackingDbContext(options);
    }
}
