/** Read-only links into Corvid Courier's tracking API and portal. */
export class CorvidTracking {
  constructor(private readonly baseUrl: string) {}

  /** Parcels the carrier has registered but not yet collected (Corvid's TODO state). */
  awaitingCollectionUrl(accountNumber: string): string {
    return `${this.baseUrl}/v2/accounts/${encodeURIComponent(accountNumber)}/parcels?state=TODO`;
  }

  parcelUrl(trackingNumber: string): string {
    return `${this.baseUrl}/v2/parcels/${encodeURIComponent(trackingNumber)}`;
  }
}
