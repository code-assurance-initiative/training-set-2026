namespace FleetOps.Infrastructure.Caching;

public static class CacheKeys
{
    public static (string, Guid) Vehicle(Guid vehicleId) => ("vehicle", vehicleId);
}

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public long SizeLimit { get; set; } = 50_000;
}
