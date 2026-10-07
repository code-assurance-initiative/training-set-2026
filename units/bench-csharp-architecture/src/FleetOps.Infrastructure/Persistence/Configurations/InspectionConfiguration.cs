using FleetOps.Domain.Inspections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetOps.Infrastructure.Persistence.Configurations;

public sealed class InspectionConfiguration : IEntityTypeConfiguration<Inspection>
{
    public void Configure(EntityTypeBuilder<Inspection> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("inspections");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasConversion(IdConverters.Inspection);
        builder.Property(i => i.VehicleId).HasConversion(IdConverters.Vehicle);
        builder.Property(i => i.FollowUpWorkOrderId).HasConversion(IdConverters.WorkOrder);
        builder.Ignore(i => i.Passed);
        builder.OwnsMany(i => i.Defects, defect =>
        {
            defect.ToTable("inspection_defects");
            defect.Property<int>("Id").ValueGeneratedOnAdd();
            defect.HasKey("Id");
            defect.Property(d => d.Severity).HasConversion<string>().HasMaxLength(16);
            defect.Property(d => d.Description).HasMaxLength(200);
            defect.Ignore(d => d.TakesVehicleOffRoad);
        });
        builder.Navigation(i => i.Defects).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
