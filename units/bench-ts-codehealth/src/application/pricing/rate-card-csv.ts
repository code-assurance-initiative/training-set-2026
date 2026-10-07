import type { ServiceLevel } from './quote.js';
import type { RateCard } from './rate-card.js';

const serviceLevels: readonly ServiceLevel[] = ['economy', 'standard', 'express'];

/**
 * Parses a rate-card export: one row per (carrier, service level, zone) with the columns
 * carrier,service_level,currency,zone,per_kg,minimum,fuel_percent,volumetric_divisor,transit_days.
 * Rows of one carrier and service level are folded into one card.
 */
export function parseRateCardCsv(text: string): RateCard[] {
  const [header, ...rows] = text.split(/\r?\n/).filter((line) => line.trim().length > 0);
  if (header?.trim() !== expectedHeader) {
    throw new RangeError('Not a rate-card export: unexpected header');
  }
  const cards = new Map<string, MutableCard>();
  rows.forEach((row, index) => {
    const cells = row.split(',').map((cell) => cell.trim());
    if (cells.length !== 9) {
      throw new RangeError(`Rate-card row ${String(index + 2)}: expected 9 columns`);
    }
    const [
      carrier = '',
      level = '',
      currency = '',
      zone = '',
      perKg,
      minimum,
      fuel,
      divisor,
      transit,
    ] = cells;
    if (!isServiceLevel(level)) {
      throw new RangeError(`Rate-card row ${String(index + 2)}: unknown service level ${level}`);
    }
    const key = `${carrier}:${level}`;
    const card = cards.get(key) ?? newCard(carrier, level, currency, fuel, divisor, transit);
    card.perKilogram[zone] = whole(perKg, index);
    card.minimum[zone] = whole(minimum, index);
    cards.set(key, card);
  });
  return [...cards.values()];
}

const expectedHeader =
  'carrier,service_level,currency,zone,per_kg,minimum,fuel_percent,volumetric_divisor,transit_days';

interface MutableCard extends RateCard {
  readonly perKilogram: Record<string, number>;
  readonly minimum: Record<string, number>;
}

function newCard(
  carrier: string,
  serviceLevel: ServiceLevel,
  currency: string,
  fuel: string | undefined,
  divisor: string | undefined,
  transit: string | undefined,
): MutableCard {
  return {
    carrier,
    serviceLevel,
    currency,
    perKilogram: {},
    minimum: {},
    fuelPercent: Number(fuel ?? '0'),
    volumetricDivisor: Number(divisor ?? '5000'),
    transitDays: Number(transit ?? '3'),
  };
}

function whole(value: string | undefined, index: number): number {
  const parsed = Number(value);
  if (!Number.isInteger(parsed) || parsed < 0) {
    throw new RangeError(`Rate-card row ${String(index + 2)}: prices are whole minor units`);
  }
  return parsed;
}

function isServiceLevel(value: string): value is ServiceLevel {
  return (serviceLevels as readonly string[]).includes(value);
}
