using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Members;
using Rentals.Lending.Domain.Reservations;

namespace Rentals.Lending.Infrastructure.Persistence.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasConversion(id => id.Value, value => new ReservationId(value));
        builder.Property(r => r.MemberId).HasConversion(id => id.Value, value => new MemberId(value));
        builder.Property(r => r.EquipmentId).HasConversion(id => id.Value, value => new EquipmentId(value));
        builder.HasIndex(r => new { r.EquipmentId, r.Status });
        builder.Ignore(r => r.DomainEvents);
    }
}
