/** An address as the public API accepts and returns it (version 1 of the contract). */
export interface AddressDto {
  readonly name: string;
  readonly company?: string | undefined;
  readonly street: string;
  readonly street2?: string | undefined;
  readonly postcode: string;
  readonly city: string;
  readonly country: string;
  readonly phone?: string | undefined;
}
