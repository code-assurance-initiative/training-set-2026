using Shipping.Rates.Core.Domain;

namespace Shipping.Rates.Core.Pricing;

/// <summary>Prices a multi-parcel shipment with one carrier: the sum of its parcels plus per-shipment rules.</summary>
public sealed class MultiParcelQuoter
{
    private const int MaxOversizePerShipment = 2;

    private readonly RateCalculator _calculator;
    private readonly SurchargePolicy _policy;

    public MultiParcelQuoter(RateCalculator calculator, SurchargePolicy policy)
    {
        _calculator = calculator;
        _policy = policy;
    }

    public Money QuoteShipment(string carrier, ServiceLevel level, IReadOnlyList<Parcel> parcels, Address destination)
    {
        if (parcels == null || parcels.Count == 0)
        {
            throw new ArgumentNullException(nameof(parcels));
        }

        var oversize = 0;
        var amounts = new decimal[parcels.Count];
        string? currency = null;
        for (var i = 0; i < parcels.Count; i++)
        {
            switch (parcels[i])
            {
                case Parcel p when level != ServiceLevel.Economy && ++oversize > MaxOversizePerShipment && p.IsOversize:
                    throw new ShipmentRejectedException($"At most {MaxOversizePerShipment} oversize parcels per {level} shipment.");
                case Parcel p:
                    var price = _calculator.QuoteParcel(carrier, level, p, destination);
                    amounts[i] = price.Amount;
                    currency = price.Currency;
                    break;
            }

            if (parcels[0].IsDangerousGoods)
            {
                amounts[i] += _policy.DangerousGoodsFee(carrier, parcels[i]);
            }
        }

        return new Money(amounts.Sum(), currency ?? "EUR").Rounded();
    }
}
