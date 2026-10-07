using FleetOps.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Infrastructure.Persistence.Configurations;

public sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("work_orders");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasConversion(IdConverters.WorkOrder);
        builder.Property(w => w.VehicleId).HasConversion(IdConverters.Vehicle);
        builder.Property(w => w.Title).HasMaxLength(200);
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(16);
        builder.OwnsMany(w => w.Lines, line =>
        {
            line.ToTable("work_order_lines");
            line.Property<int>("Id").ValueGeneratedOnAdd();
            line.HasKey("Id");
            line.Property(l => l.Kind).HasConversion<string>().HasMaxLength(16);
            line.Property(l => l.Description).HasMaxLength(200);
        });
        builder.Navigation(w => w.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
