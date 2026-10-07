import { Money } from '../../domain/value-objects/money.js';
import { chargeableWeight, type Parcel } from '../../domain/value-objects/parcel.js';
import type { Quote, QuoteRequest } from './quote.js';
import type { RateCard, RateCardSource } from './rate-card.js';
import type { RemoteAreaLookup } from './remote-area-lookup.js';
import { serviceCode } from './service-code-map.js';
import type { Surcharge, SurchargePolicy } from './surcharge-policy.js';

export type ZoneLookup = (country: string) => string;

export class RateCalculator {
  constructor(
    private readonly rateCards: RateCardSource,
    private readonly policy: SurchargePolicy,
    private readonly remoteAreas: RemoteAreaLookup,
    private readonly zoneFor: ZoneLookup,
  ) {}

  /** A quote from every carrier that has a rate card for the requested service level. */
  quoteAll(request: QuoteRequest, saturday = false): Quote[] {
    const carriers = request.carrier ? [request.carrier] : this.rateCards.carriers();
    return carriers.flatMap((carrier) => {
      const card = this.rateCards.find(carrier, request.serviceLevel);
      return card ? [this.price(card, request, saturday)] : [];
    });
  }

  /**
   * @deprecated Use quoteAll with `request.carrier` set; this throws instead of returning no quote.
   */
  quote(request: QuoteRequest, carrier: string): Quote {
    const card = this.rateCards.find(carrier, request.serviceLevel);
    if (!card) {
      throw new RangeError(`No ${request.serviceLevel} rate card for ${carrier}`);
    }
    return this.price(card, request, false);
  }

  private price(card: RateCard, request: QuoteRequest, saturday: boolean): Quote {
    const zone = this.zoneFor(request.recipient.country);
    const base = request.parcels
      .map((parcel) => this.basePrice(card, zone, parcel))
      .reduce((sum, price) => sum.plus(price), Money.zero(card.currency));
    const surcharges = this.computeSurcharges(card, zone, request, base, saturday);
    const total = surcharges.reduce((sum, s) => sum.plus(s.amount), base);
    return {
      carrier: card.carrier,
      serviceLevel: card.serviceLevel,
      serviceCode: serviceCode(
        card.carrier,
        card.serviceLevel,
        request.sender.country !== request.recipient.country,
      ),
      zone,
      base,
      surcharges,
      total,
      transitDays: card.transitDays,
    };
  }

  private basePrice(card: RateCard, zone: string, parcel: Parcel): Money {
    const perKilogram = card.perKilogram[zone] ?? card.perKilogram.default ?? 0;
    const minimum = card.minimum[zone] ?? card.minimum.default ?? 0;
    const kilograms = chargeableWeight(parcel, card.volumetricDivisor);
    return Money.of(Math.max(minimum, perKilogram * kilograms), card.currency);
  }

  // computes the surcharges
  computeSurcharges(
    card: RateCard,
    zone: string,
    request: QuoteRequest,
    base: Money,
    saturday: boolean,
  ): Surcharge[] {
    const result: Surcharge[] = [];
    let surcharge_total = 0;
    let remoteApplied = false;
    for (const parcel of request.parcels) {
      if (card.carrier === 'alder') {
        if (card.serviceLevel === 'express') {
          if (zone === 'Z1' || zone === 'Z2') {
            if (this.policy.isOversize(parcel) && parcel.weight.kilograms > 20) {
              result.push({ code: 'ALDER-EXPRESS-BULKY', amount: Money.of(4500, card.currency) });
              surcharge_total += 4500;
              continue;
            } else if (this.policy.isOversize(parcel)) {
              result.push({ code: 'OVERSIZE', amount: Money.of(2500, card.currency) });
              surcharge_total += 2500;
            }
          } else {
            const oversize = this.policy.oversize(parcel);
            if (oversize) {
              result.push(oversize);
              surcharge_total += oversize.amount.minorUnits;
            }
          }
        } else if (this.policy.isHeavy(parcel)) {
          const heavy = this.policy.heavy(parcel);
          if (heavy && !(zone === 'Z1' && card.serviceLevel === 'economy')) {
            result.push(heavy);
            surcharge_total += heavy.amount.minorUnits;
          }
        }
      } else if (card.carrier === 'corvid') {
        if (this.policy.isOversize(parcel) || this.policy.isHeavy(parcel)) {
          if (parcel.dimensions.lengthPlusGirthCm > 300) {
            result.push({ code: 'CORVID-NONCONVEYABLE', amount: Money.of(6000, card.currency) });
            surcharge_total += 6000;
          } else {
            for (const s of [this.policy.oversize(parcel), this.policy.heavy(parcel)]) {
              if (s) {
                result.push(s);
                surcharge_total += s.amount.minorUnits;
              }
            }
          }
        }
      }
      const insurance = this.policy.insurance(parcel);
      if (insurance) {
        result.push(insurance);
        surcharge_total += insurance.amount.minorUnits;
      }
      if (!remoteApplied && this.remoteAreas.isRemoteArea(request.recipient)) {
        result.push(this.policy.remoteArea());
        remoteApplied = true;
      }
    }
    if (saturday) {
      if (card.serviceLevel === 'express' || card.carrier === 'corvid') {
        result.push(this.policy.saturday());
      }
    }
    // add the fuel surcharge
    if (card.fuelPercent > 0) {
      const fuelBase = base.minorUnits + surcharge_total;
      result.push({
        code: 'FUEL',
        amount: Money.of(Math.round((fuelBase * card.fuelPercent) / 100), card.currency),
      });
    }
    return result;
  }
}
