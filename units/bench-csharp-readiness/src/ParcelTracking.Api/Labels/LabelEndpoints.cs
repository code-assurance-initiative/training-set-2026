using ParcelTracking.Api.Security;
using ParcelTracking.Core.Carriers;
using ParcelTracking.Core.Tracking;

namespace ParcelTracking.Api.Labels;

/// <summary>Shipping labels: the carrier's PDF for a parcel, streamed to the merchant's browser.</summary>
public static class LabelEndpoints
{
    public static IEndpointRouteBuilder MapLabelEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{trackingNumber}", GetLabelAsync).RequireAuthorization(AuthorizationPolicies.ReadParcels);
        return endpoints;
    }

    private static async Task<IResult> GetLabelAsync(
        string trackingNumber,
        HttpContext context,
        TrackingService tracking,
        ICarrierClient carrier,
        CancellationToken cancellationToken)
    {
        var parcel = await tracking.FindAsync(trackingNumber, cancellationToken);
        if (parcel is null || parcel.MerchantId != context.User.MerchantId())
        {
            return Results.NotFound();
        }

        var pdf = await carrier.GetLabelAsync(parcel.CarrierCode, parcel.TrackingNumber, cancellationToken);
        return pdf is null
            ? Results.Problem(statusCode: StatusCodes.Status502BadGateway, title: "The carrier did not return a label.")
            : Results.File(pdf, "application/pdf", $"{parcel.TrackingNumber}.pdf");
    }
}
