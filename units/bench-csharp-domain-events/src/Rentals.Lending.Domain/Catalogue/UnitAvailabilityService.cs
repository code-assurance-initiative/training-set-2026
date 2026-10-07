using Microsoft.EntityFrameworkCore;
using Rentals.Lending.Domain.Loans;

namespace Rentals.Lending.Domain.Catalogue;

/// <summary>Decides whether a unit can be lent out now: it must be in good condition and not already on loan.</summary>
public sealed class UnitAvailabilityService(DbContext db)
{
    public async Task<bool> IsAvailableAsync(Equipment equipment, EquipmentUnitId unitId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        if (!equipment.Unit(unitId).IsLendable)
        {
            return false;
        }

        var onLoan = await db.Set<Loan>()
            .AnyAsync(l => l.UnitId == unitId.Value && l.Status == LoanStatus.Open, cancellationToken)
            .ConfigureAwait(false);
        return !onLoan;
    }
}
