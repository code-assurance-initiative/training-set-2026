using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rentals.SharedKernel;

namespace Rentals.Lending.Infrastructure.Persistence.Configurations;

internal static class MoneyMapping
{
    public static void Configure<TOwner>(OwnedNavigationBuilder<TOwner, Money> money)
        where TOwner : class
    {
        money.Property(m => m.Amount).HasColumnType("decimal(18,2)");
        money.Property(m => m.Currency).HasMaxLength(3);
    }
}
