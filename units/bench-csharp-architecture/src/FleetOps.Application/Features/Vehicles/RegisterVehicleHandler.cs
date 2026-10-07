using FleetOps.Application.Mapping;
using FleetOps.Contracts.Vehicles;
using FleetOps.Domain.Common;
using FleetOps.Domain.Vehicles;
using Mediator;

namespace FleetOps.Application.Features.Vehicles;

public sealed record RegisterVehicleCommand(string Vin, string Registration, string Model, int OdometerKm) : ICommand<VehicleSummary>;

public sealed class RegisterVehicleHandler(IVehicleRepository vehicles, IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterVehicleCommand, VehicleSummary>
{
    public async ValueTask<VehicleSummary> Handle(RegisterVehicleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var vin = new Vin(command.Vin);
        if (await vehicles.ExistsAsync(vin, cancellationToken).ConfigureAwait(false))
        {
            throw new DomainException($"A vehicle with VIN {vin} is already registered.");
        }

        var vehicle = Vehicle.Register(vin, command.Registration, command.Model, command.OdometerKm);
        vehicles.Add(vehicle);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return vehicle.ToSummary();
    }
}
