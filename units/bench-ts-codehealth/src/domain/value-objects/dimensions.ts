import { Weight } from './weight.js';

/** Outer parcel dimensions in centimetres. */
export class Dimensions {
  private constructor(
    readonly lengthCm: number,
    readonly widthCm: number,
    readonly heightCm: number,
  ) {}

  static of(lengthCm: number, widthCm: number, heightCm: number): Dimensions {
    for (const side of [lengthCm, widthCm, heightCm]) {
      if (!Number.isFinite(side) || side <= 0) {
        throw new RangeError('Every parcel side must be a positive length in centimetres');
      }
    }
    return new Dimensions(lengthCm, widthCm, heightCm);
  }

  get longestSideCm(): number {
    return Math.max(this.lengthCm, this.widthCm, this.heightCm);
  }

  /** Length plus girth, the measure carriers use for their oversize limits. */
  get lengthPlusGirthCm(): number {
    const sides = [this.lengthCm, this.widthCm, this.heightCm].sort((a, b) => b - a);
    const [longest = 0, middle = 0, shortest = 0] = sides;
    return longest + 2 * (middle + shortest);
  }

  /** Volumetric weight for a carrier divisor (cm³ per kilogram). */
  volumetricWeight(divisor: number): Weight {
    return Weight.kilograms((this.lengthCm * this.widthCm * this.heightCm) / divisor);
  }
}
