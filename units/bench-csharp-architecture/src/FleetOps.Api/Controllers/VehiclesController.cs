using FleetOps.Api.Requests;
using FleetOps.Api.Security;
using FleetOps.Application.Features.Vehicles;
using FleetOps.Contracts.Paging;
using FleetOps.Contracts.Vehicles;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetOps.Api.Controllers;

[ApiController]
[Route("vehicles")]
public sealed class VehiclesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.FleetRead)]
    public async Task<PagedResult<VehicleSummary>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) =>
        await sender.Send(new ListVehiclesQuery(Math.Max(page, 1), Math.Clamp(pageSize, 1, 200)), cancellationToken).ConfigureAwait(false);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.FleetRead)]
    public async Task<ActionResult<VehicleSummary>> Get(Guid id, CancellationToken cancellationToken) =>
        await sender.Send(new GetVehicleQuery(id), cancellationToken).ConfigureAwait(false) is { } vehicle ? vehicle : NotFound();

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.FleetWrite)]
    public async Task<ActionResult<VehicleSummary>> Register(RegisterVehicleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var vehicle = await sender
            .Send(new RegisterVehicleCommand(request.Vin, request.Registration, request.Model, request.OdometerKm), cancellationToken)
            .ConfigureAwait(false);
        return CreatedAtAction(nameof(Get), new { id = vehicle.Id }, vehicle);
    }
}
