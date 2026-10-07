using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Maintenance;

namespace Rentals.Lending.Infrastructure.Persistence.Configurations;

internal sealed class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("maintenance_records");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasConversion(id => id.Value, value => new MaintenanceRecordId(value));
        builder.Property(r => r.UnitId).HasConversion(id => id.Value, value => new EquipmentUnitId(value));
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.EstimatedCost).HasColumnType("decimal(18,2)");
        builder.Property(r => r.OpenedAt);
        builder.Property(r => r.CompletedAt).HasField("_completedAt").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(r => r.UnitId);
    }
}
