/** A carrier API answered with an error status. */
export class CarrierError extends Error {
  constructor(
    readonly carrier: string,
    readonly status: number,
    readonly code: string,
  ) {
    super(`${carrier} answered ${String(status)} (${code})`);
    this.name = 'CarrierError';
  }
}
