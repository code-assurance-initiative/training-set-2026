/** A postal address. Country is an ISO 3166-1 alpha-2 code. */
export class Address {
  private constructor(
    readonly name: string,
    readonly lines: readonly string[],
    readonly postcode: string,
    readonly city: string,
    readonly country: string,
  ) {}

  static of(fields: {
    name: string;
    lines: readonly string[];
    postcode: string;
    city: string;
    country: string;
  }): Address {
    const name = fields.name.trim();
    const lines = fields.lines.map((line) => line.trim()).filter((line) => line.length > 0);
    if (name.length === 0 || lines.length === 0) {
      throw new RangeError('An address needs a name and at least one street line');
    }
    if (!/^[A-Z]{2}$/.test(fields.country)) {
      throw new RangeError('An address needs an ISO 3166-1 alpha-2 country code');
    }
    return new Address(name, lines, fields.postcode.trim(), fields.city.trim(), fields.country);
  }

  get isDomestic(): boolean {
    return this.country === 'DK';
  }

  /** The address as label lines, recipient first. */
  toLines(): string[] {
    return [this.name, ...this.lines, `${this.postcode} ${this.city}`, this.country];
  }
}
