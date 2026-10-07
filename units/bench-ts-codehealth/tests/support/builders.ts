import type { RateCard } from '../../src/application/pricing/rate-card.js';
import type { QuoteRequest } from '../../src/application/pricing/quote.js';
import { Address } from '../../src/domain/value-objects/address.js';
import { Dimensions } from '../../src/domain/value-objects/dimensions.js';
import { Money } from '../../src/domain/value-objects/money.js';
import type { Parcel } from '../../src/domain/value-objects/parcel.js';
import { Weight } from '../../src/domain/value-objects/weight.js';

export const copenhagen = Address.of({
  name: 'Nordlys Tea ApS',
  lines: ['Vesterbrogade 12'],
  postcode: '1620',
  city: 'København V',
  country: 'DK',
});

export const aarhus = Address.of({
  name: 'Mette Holm',
  lines: ['Klostergade 3', '2. tv'],
  postcode: '8000',
  city: 'Aarhus C',
  country: 'DK',
});

export const hamburg = Address.of({
  name: 'Jonas Weber',
  lines: ['Lange Reihe 41'],
  postcode: '20099',
  city: 'Hamburg',
  country: 'DE',
});

export function parcel(
  grams = 2_000,
  sides: [number, number, number] = [30, 20, 10],
  declaredValue?: number,
): Parcel {
  return {
    weight: Weight.grams(grams),
    dimensions: Dimensions.of(...sides),
    ...(declaredValue === undefined ? {} : { declaredValue: Money.of(declaredValue, 'DKK') }),
  };
}

export function quoteRequest(overrides: Partial<QuoteRequest> = {}): QuoteRequest {
  return {
    serviceLevel: 'standard',
    sender: copenhagen,
    recipient: aarhus,
    parcels: [parcel()],
    ...overrides,
  };
}

export function rateCard(overrides: Partial<RateCard> = {}): RateCard {
  return {
    carrier: 'alder',
    serviceLevel: 'standard',
    currency: 'DKK',
    perKilogram: { Z1: 1_000, Z2: 1_800, default: 3_000 },
    minimum: { Z1: 4_900, Z2: 8_900, default: 12_900 },
    fuelPercent: 0,
    volumetricDivisor: 5_000,
    transitDays: 2,
    ...overrides,
  };
}

/** Request bodies as a client sends them. */
export const addressBody = {
  name: 'Mette Holm',
  street: 'Klostergade 3',
  postcode: '8000',
  city: 'Aarhus C',
  country: 'DK',
};

export const senderBody = {
  name: 'Nordlys Tea ApS',
  street: 'Vesterbrogade 12',
  postcode: '1620',
  city: 'København V',
  country: 'DK',
};

export const parcelBody = { weightGrams: 2_000, lengthCm: 30, widthCm: 20, heightCm: 10 };
