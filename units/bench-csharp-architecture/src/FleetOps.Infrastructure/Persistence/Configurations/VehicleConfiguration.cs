using FleetOps.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("vehicles");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasConversion(IdConverters.Vehicle);
        builder.Property(v => v.Vin).HasConversion(v => v.Value, value => new Vin(value)).HasMaxLength(17);
        builder.HasIndex(v => v.Vin).IsUnique();
        builder.Property(v => v.Registration).HasMaxLength(16);
        builder.Property(v => v.Model).HasMaxLength(80);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(16);
    }
}
