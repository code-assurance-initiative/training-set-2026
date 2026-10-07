using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Carriers;

/// <summary>The operations every carrier integration provides.</summary>
public interface ICarrierAdapter
{
    /// <summary>The carrier's code in upper case, e.g. <c>ALDER</c>.</summary>
    string Code { get; }

    CarrierCapabilities Capabilities { get; }

    Task<IReadOnlyList<CarrierRate>> GetRatesAsync(QuoteRequest request, CancellationToken cancellationToken);

    Task<CarrierLabel> CreateLabelAsync(QuoteRequest request, ServiceLevel level, CancellationToken cancellationToken);

    Task VoidLabelAsync(string trackingNumber, CancellationToken cancellationToken);

    Task<TrackingStatus> TrackAsync(string trackingNumber, CancellationToken cancellationToken);

    Task SchedulePickupAsync(Address pickupAddress, DateOnly date, CancellationToken cancellationToken);
}
