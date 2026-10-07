import { carrierPrefixes } from '../../infrastructure/carriers/carrier-prefixes.js';

/** A carrier tracking number: carrier prefix plus ten to fourteen digits. */
export class TrackingNumber {
  private constructor(readonly value: string) {}

  static parse(value: string): TrackingNumber | undefined {
    const pattern = /^[A-Z]{3}\d{10,14}$/;
    const normalised = value.trim().toUpperCase();
    return pattern.test(normalised) ? new TrackingNumber(normalised) : undefined;
  }

  /** The carrier that issued the number, from its prefix. */
  get carrier(): string | undefined {
    const prefix = this.value.slice(0, 3);
    return Object.entries(carrierPrefixes).find(([, prefixes]) => prefixes.includes(prefix))?.[0];
  }

  toString(): string {
    return this.value;
  }
}

export function trackingUrl(tracking: TrackingNumber, baseUrl: string): string {
  return `${baseUrl.replace(/\/$/, '')}/track/${encodeURIComponent(tracking.value)}`;
}
