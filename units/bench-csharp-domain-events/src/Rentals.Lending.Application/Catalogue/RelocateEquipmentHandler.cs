using Rentals.Lending.Domain.Catalogue;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Catalogue;

/// <summary>Moves an equipment type to the branch and shelf it is now collected from.</summary>
public sealed class RelocateEquipmentHandler(IEquipmentRepository equipment, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<RelocateEquipmentCommand>
{
    public async Task HandleAsync(RelocateEquipmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var item = await equipment.GetAsync(command.EquipmentId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Equipment", command.EquipmentId);
        item.Relocate(new StorageLocation(command.Branch, command.Shelf), clock.GetUtcNow());
        await equipment.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
