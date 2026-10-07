/** A kind of item the warehouse stocks. */
export interface Sku {
  readonly code: string;
  readonly description: string;
  readonly unitOfMeasure: string;
}

/** A storage place (aisle-rack-shelf) with a capacity in units. */
export interface BinLocation {
  readonly code: string;
  readonly zone: string;
  readonly capacity: number;
}
