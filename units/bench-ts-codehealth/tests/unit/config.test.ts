import { describe, expect, it } from 'vitest';
import { ConfigurationError, loadConfig } from '../../src/config.js';

const required = {
  JWT_ISSUER: 'https://identity.parcel-rates.test/realms/shipping',
  JWT_PUBLIC_KEY: 'pem',
  ALDER_API_KEY: 'alder-key-0123456789',
  CORVID_ACCOUNT: 'A1',
  CORVID_TOKEN: 'corvid-token-0123456789',
  WEBHOOK_SECRET: 'webhook-secret-0123456789-0123456789',
};

describe('loadConfig', () => {
  it('applies the defaults', () => {
    const config = loadConfig(required);
    expect(config.port).toBe(8080);
    expect(config.environment).toBe('production');
    expect(config.carriers.cutoffs).toEqual({ alder: '16:00', corvid: '15:30' });
    expect(config.labels.fromAddress).toBeUndefined();
  });

  it('names every problem without echoing values', () => {
    try {
      loadConfig({ ...required, PORT: 'eighty', CORVID_TOKEN: 'short' });
      expect.unreachable();
    } catch (error) {
      expect(error).toBeInstanceOf(ConfigurationError);
      expect((error as ConfigurationError).problems.map((p) => p.split(':')[0])).toEqual([
        'PORT',
        'CORVID_TOKEN',
      ]);
      expect((error as Error).message).not.toContain('short');
    }
  });
});
