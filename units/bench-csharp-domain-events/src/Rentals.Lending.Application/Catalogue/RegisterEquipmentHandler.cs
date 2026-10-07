using Rentals.Lending.Domain.Catalogue;
using Rentals.Messaging;
using Rentals.SharedKernel;

namespace Rentals.Lending.Application.Catalogue;

/// <summary>Adds an equipment type and its units to the catalogue. The caller chooses the id, so a retry is a no-op.</summary>
public sealed class RegisterEquipmentHandler(IEquipmentRepository equipment, IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterEquipmentCommand>
{
    public async Task HandleAsync(RegisterEquipmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (await equipment.GetAsync(command.EquipmentId, cancellationToken).ConfigureAwait(false) is not null)
        {
            return;
        }

        var item = new Equipment(command.EquipmentId, command.Name, command.DailyRate, command.ReplacementValue);
        foreach (var serial in command.SerialNumbers)
        {
            item.AddUnit(new SerialNumber(serial));
        }

        await equipment.AddAsync(item, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
