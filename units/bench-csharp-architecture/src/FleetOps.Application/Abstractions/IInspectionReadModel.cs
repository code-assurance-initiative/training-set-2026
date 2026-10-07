using FleetOps.Contracts.Inspections;

namespace FleetOps.Application.Abstractions;

public interface IInspectionReadModel
{
    Task<InspectionSummary?> GetInspectionAsync(Guid inspectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InspectionSummary>> ListForVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InspectionSummary>> ListFailedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    Task<int> CountSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    Task<int> CountFailedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    Task<DateTimeOffset?> LastInspectedAtAsync(Guid vehicleId, CancellationToken cancellationToken);
}
