import { describe, expect, it } from 'vitest';
import { ConfigurationError, loadConfig } from '../../src/config.js';

const required = {
  JWT_ISSUER: 'https://identity.warehouse.test/realms/stock',
  JWT_PUBLIC_KEY: 'public key in PEM form',
};

function captureError(action: () => unknown): unknown {
  try {
    action();
  } catch (error) {
    return error;
  }
  throw new Error('expected the action to throw');
}

describe('loadConfig', () => {
  it('applies production defaults', () => {
    expect(loadConfig(required)).toEqual({
      environment: 'production',
      host: '127.0.0.1',
      port: 8080,
      logLevel: 'info',
      trustProxy: 'loopback',
      jwt: {
        issuer: required.JWT_ISSUER,
        audience: 'warehouse-stock-api',
        publicKeyPem: required.JWT_PUBLIC_KEY,
      },
      reservations: { defaultHoldMinutes: 15, maximumHoldMinutes: 1_440, sweepIntervalMs: 30_000 },
    });
  });

  it('reads every setting from the environment', () => {
    const config = loadConfig({
      ...required,
      NODE_ENV: 'development',
      PORT: '9000',
      LOG_LEVEL: 'debug',
      RESERVATION_SWEEP_SECONDS: '5',
    });

    expect(config).toMatchObject({
      environment: 'development',
      port: 9000,
      logLevel: 'debug',
      reservations: { sweepIntervalMs: 5_000 },
    });
  });

  it('names every invalid setting without echoing any value', () => {
    const error = captureError(() => loadConfig({ JWT_PUBLIC_KEY: 'do-not-echo', PORT: 'eighty' }));

    expect(error).toBeInstanceOf(ConfigurationError);
    const { problems, message } = error as ConfigurationError;
    expect(problems.map((problem) => problem.split(':')[0])).toEqual(['PORT', 'JWT_ISSUER']);
    expect(message).not.toContain('do-not-echo');
    expect(message).not.toContain('eighty');
  });

  it('requires an HTTPS issuer', () => {
    expect(() => loadConfig({ ...required, JWT_ISSUER: 'http://identity.warehouse.test' })).toThrow(
      ConfigurationError,
    );
  });

  it('refuses a default hold longer than the maximum', () => {
    expect(() =>
      loadConfig({
        ...required,
        RESERVATION_DEFAULT_HOLD_MINUTES: '120',
        RESERVATION_MAX_HOLD_MINUTES: '60',
      }),
    ).toThrow(/must not exceed RESERVATION_MAX_HOLD_MINUTES/);
  });
});
