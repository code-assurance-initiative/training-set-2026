using FleetOps.Application.Abstractions;
using FleetOps.Contracts.Paging;
using FleetOps.Contracts.Vehicles;
using Mediator;

namespace FleetOps.Application.Features.Vehicles;

public sealed record ListVehiclesQuery(int Page, int PageSize) : IQuery<PagedResult<VehicleSummary>>;

public sealed class ListVehiclesHandler(IVehicleReadModel vehicles) : IQueryHandler<ListVehiclesQuery, PagedResult<VehicleSummary>>
{
    public async ValueTask<PagedResult<VehicleSummary>> Handle(ListVehiclesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await vehicles.ListVehiclesAsync(query.Page, query.PageSize, cancellationToken).ConfigureAwait(false);
    }
}
