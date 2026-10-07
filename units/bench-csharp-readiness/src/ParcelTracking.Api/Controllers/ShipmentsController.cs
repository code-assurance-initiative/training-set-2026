using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParcelTracking.Api.Contracts;
using ParcelTracking.Api.Security;
using ParcelTracking.Core.Tracking;

namespace ParcelTracking.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize(Policy = AuthorizationPolicies.WriteParcels)]
public sealed class ShipmentsController(TrackingService tracking) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ParcelResponse>> Register(RegisterShipmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var parcel = await tracking.RegisterAsync(request.TrackingNumber, User.MerchantId(), request.CarrierCode, request.DestinationPostalCode, cancellationToken);
        if (parcel is null)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: $"Parcel {request.TrackingNumber} is already registered.");
        }

        return Created($"/api/parcels/{Uri.EscapeDataString(parcel.TrackingNumber)}", ParcelResponse.From(parcel, carrier: null));
    }
}
