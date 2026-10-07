namespace ParcelTracking.Core.Carriers;

public interface ICarrierClient
{
    Task<IReadOnlyList<CarrierScan>> GetScansAsync(string carrierCode, string trackingNumber, DateTimeOffset since, CancellationToken cancellationToken);

    Task<IReadOnlyList<CarrierInfo>> GetCarriersAsync(CancellationToken cancellationToken);

    Task<byte[]?> GetLabelAsync(string carrierCode, string trackingNumber, CancellationToken cancellationToken);
}
