/** A parcel weight in grams. */
export class Weight {
  private constructor(readonly grams: number) {}

  static grams(value: number): Weight {
    if (!Number.isInteger(value) || value <= 0) {
      throw new RangeError('A weight is a positive whole number of grams');
    }
    return new Weight(value);
  }

  static kilograms(value: number): Weight {
    return Weight.grams(Math.round(value * 1000));
  }

  get kilograms(): number {
    return this.grams / 1000;
  }

  /** Whole kilograms, rounded up: carriers bill per started kilogram. */
  get billableKilograms(): number {
    return Math.ceil(this.grams / 1000);
  }

  max(other: Weight): Weight {
    return other.grams > this.grams ? other : this;
  }
}
