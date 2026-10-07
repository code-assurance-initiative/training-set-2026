using FleetOps.Infrastructure.FuelCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Infrastructure.Persistence.Configurations;

public sealed class FuelTransactionConfiguration : IEntityTypeConfiguration<FuelTransaction>
{
    public void Configure(EntityTypeBuilder<FuelTransaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("fuel_transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.ProviderReference).HasMaxLength(64);
        builder.HasIndex(t => t.ProviderReference).IsUnique();
        builder.Property(t => t.Vin).HasMaxLength(17);
        builder.Property(t => t.Station).HasMaxLength(120);
    }
}
