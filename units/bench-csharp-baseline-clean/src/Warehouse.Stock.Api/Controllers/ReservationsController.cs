using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Stock.Api.Contracts;
using Warehouse.Stock.Api.Errors;
using Warehouse.Stock.Api.Security;
using Warehouse.Stock.Application.Reservations;

namespace Warehouse.Stock.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public sealed class ReservationsController(ReservationService reservations) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ReservationsWrite)]
    public async Task<ActionResult<ReservationResponse>> Create(
        CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await reservations.CreateAsync(request.ToNewReservation(), cancellationToken).ConfigureAwait(false);
        return this.ToCreated(result, ReservationResponse.From, nameof(Get), reservation => new { id = reservation.Id });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StockRead)]
    public async Task<ActionResult<ReservationResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await reservations.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return this.ToOk(result, ReservationResponse.From);
    }

    [HttpPost("{id:guid}/release")]
    [Authorize(Policy = AuthorizationPolicies.ReservationsWrite)]
    public async Task<ActionResult<ReservationResponse>> Release(Guid id, CancellationToken cancellationToken)
    {
        var result = await reservations.ReleaseAsync(id, cancellationToken).ConfigureAwait(false);
        return this.ToOk(result, ReservationResponse.From);
    }

    [HttpPost("{id:guid}/fulfilment")]
    [Authorize(Policy = AuthorizationPolicies.ReservationsWrite)]
    public async Task<ActionResult<ReservationResponse>> Fulfil(Guid id, CancellationToken cancellationToken)
    {
        var result = await reservations.FulfilAsync(id, cancellationToken).ConfigureAwait(false);
        return this.ToOk(result, ReservationResponse.From);
    }
}
