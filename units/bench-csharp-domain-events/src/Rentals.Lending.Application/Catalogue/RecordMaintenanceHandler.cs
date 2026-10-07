using Rentals.Lending.Domain.Maintenance;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Catalogue;

/// <summary>Opens a maintenance record for a unit. The caller chooses the record id, so a retry is a no-op.</summary>
public sealed class RecordMaintenanceHandler(IMaintenanceRecordRepository records, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<RecordMaintenanceCommand>
{
    public async Task HandleAsync(RecordMaintenanceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (await records.GetAsync(command.RecordId, cancellationToken).ConfigureAwait(false) is not null)
        {
            return;
        }

        var record = new MaintenanceRecord(command.RecordId, command.UnitId, command.Description, command.EstimatedCost, clock.GetUtcNow());
        await records.AddAsync(record, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
