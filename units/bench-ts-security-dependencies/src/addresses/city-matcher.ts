import stringSimilarity from 'string-similarity';

/** Below this Dice coefficient a spelling is not taken to mean any service-area city. */
const minimumRating = 0.6;

export interface CityMatch {
  readonly city: string;
  readonly rating: number;
}

/**
 * Matches the city a shipper typed ("Kobenhavn S", "aarhus c", "Arhus") to one of the depot's
 * service-area cities. Comparison ignores case, punctuation and the Danish letters' ASCII spellings.
 */
export class CityMatcher {
  readonly #cities: readonly string[];
  readonly #normalised: readonly string[];

  constructor(serviceArea: readonly string[]) {
    if (serviceArea.length === 0) {
      throw new Error('A service area needs at least one city.');
    }
    this.#cities = serviceArea;
    this.#normalised = serviceArea.map(normalise);
  }

  /** The best service-area city for `typed`, or undefined when none is close enough. */
  match(typed: string): CityMatch | undefined {
    const candidate = normalise(typed);
    if (candidate.length === 0) {
      return undefined;
    }
    const exact = this.#normalised.indexOf(candidate);
    if (exact >= 0) {
      return { city: this.#cityAt(exact), rating: 1 };
    }
    const { bestMatch, bestMatchIndex } = stringSimilarity.findBestMatch(
      candidate,
      this.#normalised.slice(),
    );
    return bestMatch.rating >= minimumRating
      ? { city: this.#cityAt(bestMatchIndex), rating: bestMatch.rating }
      : undefined;
  }

  #cityAt(index: number): string {
    const city = this.#cities[index];
    if (city === undefined) {
      throw new RangeError(`No service-area city at index ${index}.`);
    }
    return city;
  }
}

function normalise(city: string): string {
  return city
    .toLowerCase()
    .replaceAll('æ', 'ae')
    .replaceAll('ø', 'oe')
    .replaceAll('å', 'aa')
    .replaceAll(/[^a-z0-9 ]/g, ' ')
    .replaceAll(/\s+/g, ' ')
    .trim();
}
