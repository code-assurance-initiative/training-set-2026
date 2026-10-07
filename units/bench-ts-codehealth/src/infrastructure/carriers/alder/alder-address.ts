/** An address in Alder Parcel's v2 wire format (field names fixed by the carrier's schema). */
export interface AlderAddress {
  readonly name: string;
  readonly company?: string | undefined;
  readonly street: string;
  readonly street2?: string | undefined;
  readonly postcode: string;
  readonly city: string;
  readonly country: string;
  readonly phone?: string | undefined;
}
