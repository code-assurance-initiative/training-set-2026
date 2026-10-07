namespace Shipping.Rates.Core.Carriers.Alder;

/// <summary>Alder Parcel's wire format for a shipment order (v2 shipments API).</summary>
public sealed record AlderShipment(
    AlderAddress Sender,
    AlderAddress Recipient,
    string Product,
    IReadOnlyList<AlderPiece> Pieces,
    decimal InsuredValue);

public sealed record AlderPiece(decimal WeightKg, int LengthCm, int WidthCm, int HeightCm);

public sealed record AlderLabelResponse(string TrackingNumber, string LabelZpl);
