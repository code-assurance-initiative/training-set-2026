using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentals.Lending.Domain.Catalogue;

namespace Rentals.Lending.Infrastructure.Persistence.Configurations;

internal sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("equipment");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasConversion(id => id.Value, value => new EquipmentId(value));
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.OwnsOne(e => e.DailyRate, MoneyMapping.Configure);
        builder.OwnsOne(e => e.ReplacementValue, MoneyMapping.Configure);
        builder.OwnsOne(e => e.Location, location =>
        {
            location.Property(l => l.Branch).HasMaxLength(100);
            location.Property(l => l.Shelf).HasMaxLength(20);
        });
        builder.Ignore(e => e.DomainEvents);

        builder.OwnsMany(e => e.Units, unit =>
        {
            unit.ToTable("equipment_units");
            unit.WithOwner().HasForeignKey("EquipmentId");
            unit.HasKey(u => u.Id);
            unit.Property(u => u.Id).HasConversion(id => id.Value, value => new EquipmentUnitId(value)).ValueGeneratedNever();
            unit.Property(u => u.SerialNumber).HasConversion(s => s.Value, value => new SerialNumber(value)).HasMaxLength(40);
            unit.HasIndex(u => u.SerialNumber).IsUnique();
        });
        builder.Navigation(e => e.Units).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_units");
    }
}
