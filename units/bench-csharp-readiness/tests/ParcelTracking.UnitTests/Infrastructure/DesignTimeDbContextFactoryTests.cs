using Microsoft.EntityFrameworkCore;
using ParcelTracking.Infrastructure.Persistence;

namespace ParcelTracking.UnitTests.Infrastructure;

public sealed class DesignTimeDbContextFactoryTests
{
    [Fact]
    public void The_migrations_tooling_gets_a_PostgreSQL_context()
    {
        using var context = new DesignTimeDbContextFactory().CreateDbContext([]);

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.NotNull(context.Model.FindEntityType(typeof(ParcelTracking.Core.Parcels.Parcel)));
    }
}
