import { describe, expect, it } from 'vitest';
import { ConfigurationError, loadConfig } from '../../src/config.js';
import { createCarrierSigner } from '../support/carrier-keys.js';
import { testEnvironment } from '../support/test-config.js';
import { createTokenIssuer } from '../support/test-tokens.js';

const tokens = createTokenIssuer();
const carrier = createCarrierSigner();

describe('configuration', () => {
  it('reads a complete environment with defaults', () => {
    const config = loadConfig(testEnvironment(tokens.publicKeyPem, carrier.publicKey));

    expect(config).toMatchObject({
      environment: 'test',
      host: '127.0.0.1',
      port: 0,
      shutdownGraceMs: 10_000,
      terminalTokens: { issuer: 'depot-identity', audience: 'depot-dispatch' },
      linehaul: { baseUrl: 'https://partners.linehaul.test' },
      labelPrinterDpi: 203,
    });
    expect(config.carrierStatusPublicKey).toEqual(carrier.publicKey);
  });

  it('names every problem without echoing values', () => {
    const environment = testEnvironment(tokens.publicKeyPem, carrier.publicKey, {
      LINEHAUL_BASE_URL: 'http://partners.linehaul.test',
      CARRIER_STATUS_PUBLIC_KEY: Buffer.from('too short').toString('base64'),
      LABEL_PRINTER_DPI: '96',
    });

    expect(() => loadConfig(environment)).toThrow(ConfigurationError);
    try {
      loadConfig(environment);
    } catch (error) {
      const { problems } = error as ConfigurationError;
      expect(problems.map((p) => p.split(':')[0])).toEqual([
        'LINEHAUL_BASE_URL',
        'CARRIER_STATUS_PUBLIC_KEY',
        'LABEL_PRINTER_DPI',
      ]);
      expect(problems.join(' ')).not.toContain('too short');
    }
  });
});
