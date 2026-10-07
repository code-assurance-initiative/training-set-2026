using Shipping.Rates.Core.Domain;
using Shipping.Rates.Core.Pricing.Generated;

namespace Shipping.Rates.Core.Pricing;

/// <summary>Prices one parcel with one carrier: tariff, service-level factor and surcharges.</summary>
public sealed class RateCalculator
{
    private readonly IRateCardProvider _rateCards;
    private readonly SurchargePolicy _policy;
    private readonly RemoteAreaLookup _remoteAreas;

    public RateCalculator(IRateCardProvider rateCards, SurchargePolicy policy, RemoteAreaLookup remoteAreas)
    {
        _rateCards = rateCards;
        _policy = policy;
        _remoteAreas = remoteAreas;
    }

    public Money QuoteParcel(string carrier, ServiceLevel level, Parcel parcel, Address destination)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        ArgumentNullException.ThrowIfNull(destination);
        var card = _rateCards.GetCard(carrier);
        var grams = DimensionalWeight.ChargeableGrams(parcel, card.VolumetricDivisor);
        var zone = CountryZones.ZoneFor(destination.CountryCode);
        var tariff = card.BaseFee + card.PerKgFor(zone) * DimensionalWeight.ChargeableKilograms(grams);
        var basePrice = new Money(tariff * LevelFactor(level), card.Currency);
        return (basePrice + ComputeSurcharges(carrier, level, parcel, destination, basePrice)).Rounded();
    }

    private static decimal LevelFactor(ServiceLevel level) => level switch
    {
        ServiceLevel.Economy => 0.85m,
        ServiceLevel.Standard => 1.0m,
        ServiceLevel.Express => 1.6m,
        ServiceLevel.Overnight => 2.4m,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };

    public Money ComputeSurcharges(string carrier, ServiceLevel level, Parcel parcel, Address destination, Money basePrice)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        ArgumentNullException.ThrowIfNull(destination);
        decimal surcharge_total = 0;
        var cc = destination.CountryCode;
        var zone = CountryZones.ZoneFor(cc);
        bool isRmt = false;
        // check if the destination is remote
        if (_remoteAreas.IsRemoteArea(cc, destination.PostalCode))
        {
            isRmt = true;
        }

        if (carrier == "ALDER")
        {
            if (level == ServiceLevel.Express || level == ServiceLevel.Overnight)
            {
                if (zone >= 4)
                {
                    surcharge_total += _policy.FuelSurcharge(carrier, basePrice.Amount) * 1.5m;
                    if (parcel.IsOversize)
                    {
                        // oversize counts double outside the EU
                        surcharge_total += _policy.OversizeFee(carrier) * 2;
                    }
                }
                else
                {
                    surcharge_total += _policy.FuelSurcharge(carrier, basePrice.Amount);
                    if (parcel.IsOversize && zone > 1)
                    {
                        surcharge_total += _policy.OversizeFee(carrier);
                    }
                }
            }
            else
            {
                surcharge_total += _policy.FuelSurcharge(carrier, basePrice.Amount);
                if (parcel.IsOversize)
                {
                    surcharge_total += _policy.OversizeFee(carrier);
                }
            }

            if (isRmt && level != ServiceLevel.Economy)
            {
                surcharge_total += _policy.RemoteAreaFee(carrier);
            }
        }
        else if (carrier == "CORVID")
        {
            // add the fuel surcharge
            surcharge_total += _policy.FuelSurcharge(carrier, basePrice.Amount);
            if (parcel.IsDangerousGoods)
            {
                if (zone > 2 || level == ServiceLevel.Overnight)
                {
                    throw new ShipmentRejectedException("Corvid carries dangerous goods only within zones 1-2 and never overnight.");
                }

            }

            if (parcel.IsOversize)
            {
                if (parcel.WeightGrams > 20_000 && level != ServiceLevel.Economy)
                {
                    surcharge_total += _policy.OversizeFee(carrier) + _policy.HeavyFee(carrier);
                }
                else
                {
                    surcharge_total += _policy.OversizeFee(carrier);
                }
            }

            if (isRmt)
            {
                surcharge_total += _policy.RemoteAreaFee(carrier);
            }
        }

        if (parcel.WeightGrams > 25_000 && carrier != "CORVID")
        {
            surcharge_total += _policy.HeavyFee(carrier);
        }

        if (zone >= 3 && CountryZones.RequiresCustomsDeclaration(cc))
        {
            surcharge_total += _policy.CustomsFee(carrier);
        }

        return new Money(Math.Round(surcharge_total, 2), basePrice.Currency);
    }

    public Money InsuranceFor(string carrier, decimal insuredValue)
    {
        var card = _rateCards.GetCard(carrier);
        return new Money(_policy.InsuranceFee(carrier, insuredValue), card.Currency).Rounded();
    }

    [Obsolete("Use QuoteParcel, which charges dimensional weight, the service level and surcharges.")]
    public Money Quote(string carrier, int weightGrams, string countryCode)
    {
        var card = _rateCards.GetCard(carrier);
#if LEGACY_ZONE_PRICING
        var zone = countryCode == "DE" ? 1 : 3;
#else
        var zone = CountryZones.ZoneFor(countryCode);
#endif
        var tariff = card.BaseFee + card.PerKgFor(zone) * DimensionalWeight.ChargeableKilograms(weightGrams);
        return new Money(tariff, card.Currency).Rounded();
    }
}

public sealed class ShipmentRejectedException(string reason) : Exception(reason);
