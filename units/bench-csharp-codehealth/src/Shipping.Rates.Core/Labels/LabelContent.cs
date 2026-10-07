using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Labels;

/// <summary>Everything printed on one parcel's label.</summary>
public sealed record LabelContent(
    string LabelId,
    string Carrier,
    string ServiceCode,
    string TrackingNumber,
    Address Sender,
    IReadOnlyList<string> RecipientLines,
    Parcel Parcel,
    int PieceNumber,
    int PieceCount,
    DateOnly ShipDate,
    string? Reference,
    bool CustomsRequired,
    decimal DeclaredValue,
    string Currency);
