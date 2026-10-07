import { randomBytes } from 'node:crypto';

/** A complete, valid environment for the service; secrets are random per call and authenticate nothing. */
export function testEnvironment(
  publicKeyPem: string,
  carrierPublicKey: Uint8Array,
  overrides: Readonly<Record<string, string>> = {},
): Record<string, string> {
  return {
    NODE_ENV: 'test',
    PORT: '0',
    LOG_LEVEL: 'silent',
    TERMINAL_TOKEN_PUBLIC_KEY: publicKeyPem,
    LINEHAUL_BASE_URL: 'https://partners.linehaul.test',
    LINEHAUL_API_KEY: randomBytes(16).toString('hex'),
    GEOCODING_API_KEY: randomBytes(16).toString('hex'),
    CARRIER_STATUS_PUBLIC_KEY: Buffer.from(carrierPublicKey).toString('base64'),
    ...overrides,
  };
}
