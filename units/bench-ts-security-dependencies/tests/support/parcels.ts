import type { Parcel } from '../../src/dispatch/dispatch-run.js';

export function parcel(
  trackingNumber: string,
  city: string,
  street = 'Søndergade 1',
  postcode = '8000',
): Parcel {
  return {
    trackingNumber,
    recipient: `Recipient ${trackingNumber}`,
    address: { street, postcode, city },
    weightKg: 2.5,
  };
}
