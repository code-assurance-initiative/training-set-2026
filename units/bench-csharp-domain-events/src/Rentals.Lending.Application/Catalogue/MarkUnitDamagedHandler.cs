using Rentals.Contracts.IntegrationEvents;
using Rentals.Lending.Domain.Catalogue;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Catalogue;

/// <summary>Withdraws a unit reported damaged or lost from lending. Setting the condition twice changes nothing.</summary>
public sealed class MarkUnitDamagedHandler(IEquipmentRepository equipment, IUnitOfWork unitOfWork)
    : IIntegrationEventHandler<EquipmentDamageReportedIntegrationEvent>
{
    public async Task HandleAsync(
        EquipmentDamageReportedIntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var item = await equipment.GetAsync(new EquipmentId(integrationEvent.EquipmentId), cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Equipment", integrationEvent.EquipmentId);
        item.ReportDamage(new EquipmentUnitId(integrationEvent.UnitId), integrationEvent.Condition);
        await equipment.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
