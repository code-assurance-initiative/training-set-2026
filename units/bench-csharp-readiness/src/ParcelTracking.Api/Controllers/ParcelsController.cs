using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParcelTracking.Api.Contracts;
using ParcelTracking.Api.Security;
using ParcelTracking.Core.Tracking;
using ParcelTracking.Infrastructure.Carriers;

namespace ParcelTracking.Api.Controllers;

[ApiController]
[Route("api/parcels")]
[Authorize(Policy = AuthorizationPolicies.ReadParcels)]
public sealed class ParcelsController(TrackingService tracking, CarrierDirectory carriers) : ControllerBase
{
    [HttpGet("{trackingNumber}")]
    public async Task<ActionResult<ParcelResponse>> Get(string trackingNumber, CancellationToken cancellationToken)
    {
        var parcel = await tracking.FindAsync(trackingNumber, cancellationToken);
        if (parcel is null || parcel.MerchantId != User.MerchantId())
        {
            return NotFound();
        }

        var carrier = await carriers.FindAsync(parcel.CarrierCode, cancellationToken);
        return ParcelResponse.From(parcel, carrier);
    }

    [HttpPost("{trackingNumber}/redirect")]
    [Authorize(Policy = AuthorizationPolicies.WriteParcels)]
    public async Task<IActionResult> RedirectToPickupPoint(string trackingNumber, RedirectParcelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parcel = await tracking.FindAsync(trackingNumber, cancellationToken);
        if (parcel is null || parcel.MerchantId != User.MerchantId())
        {
            return NotFound();
        }

        var outcome = await tracking.RedirectAsync(trackingNumber, request.PickupPointId, request.HoldUntil, request.Note, cancellationToken);
        return outcome switch
        {
            RedirectOutcome.Redirected => NoContent(),
            RedirectOutcome.AlreadyCompleted => Problem(statusCode: StatusCodes.Status409Conflict, title: "The parcel has already been delivered or returned."),
            _ => NotFound(),
        };
    }

    [HttpPost("{trackingNumber}/refresh")]
    [Authorize(Policy = AuthorizationPolicies.WriteParcels)]
    public async Task<ActionResult<ParcelResponse>> Refresh(string trackingNumber, CancellationToken cancellationToken)
    {
        var parcel = await tracking.FindAsync(trackingNumber, cancellationToken);
        if (parcel is null || parcel.MerchantId != User.MerchantId())
        {
            return NotFound();
        }

        await tracking.RefreshAsync(parcel, cancellationToken);
        var carrier = await carriers.FindAsync(parcel.CarrierCode, cancellationToken);
        return ParcelResponse.From(parcel, carrier);
    }
}
