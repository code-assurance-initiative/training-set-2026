using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Lending.Domain.Loans;

namespace Rentals.Lending.Infrastructure.Persistence.Configurations;

internal sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("loans");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasConversion(id => id.Value, value => new LoanId(value));
        builder.Property(l => l.EquipmentId).HasConversion(id => id.Value, value => new EquipmentId(value));
        builder.HasOne(l => l.Borrower).WithMany().HasForeignKey("BorrowerId").IsRequired();
        builder.Navigation(l => l.Borrower).HasField("_borrower").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        builder.HasIndex(l => new { l.UnitId, l.Status });

        // At most one open loan per unit, enforced by the database: two concurrent checkouts of the same unit (a client
        // retrying a request still in flight) cannot both commit.
        builder.HasIndex(l => l.UnitId).IsUnique().HasFilter($"\"Status\" = {(int)LoanStatus.Open}").HasDatabaseName("ux_loans_open_unit");
        builder.Ignore(l => l.DomainEvents);
    }
}
