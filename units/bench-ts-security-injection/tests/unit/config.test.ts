import { describe, expect, it } from 'vitest';
import { ConfigurationError, loadConfig } from '../../src/config.js';

const required = {
  JWT_ISSUER: 'https://identity.archive.test/realms/records',
  JWT_PUBLIC_KEY: 'public key in PEM form',
  DATABASE_URL: 'postgres://archive@db.internal:5432/archive',
  MONGODB_URL: 'mongodb://mongo.internal:27017/archive',
  CRM_BASE_URL: 'https://crm.internal.test/api',
  MAIL_RELAY_URL: 'https://mail-relay.internal.test',
  PSEUDONYM_KEY: 'k'.repeat(48),
  PUBLIC_BASE_URL: 'https://archive.example.org',
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
  it('applies production defaults and derives the storage layout', () => {
    const config = loadConfig(required);

    expect(config).toMatchObject({
      environment: 'production',
      host: '127.0.0.1',
      port: 8080,
      jwt: { audience: 'archive-search-api' },
      partnerFeedHosts: [],
      storage: {
        originals: '/var/lib/archive/originals',
        attachments: '/var/lib/archive/attachments',
        templates: '/var/lib/archive/templates',
        exports: '/var/lib/archive/exports',
        auditLog: '/var/lib/archive/audit/exports.log',
      },
      conversion: {
        sofficePath: '/usr/bin/soffice',
        magickPath: '/usr/bin/magick',
        timeoutMs: 120_000,
      },
    });
  });

  it('reads the partner host list and the storage root', () => {
    const config = loadConfig({
      ...required,
      PARTNER_FEED_HOSTS: ' Feeds.Partner-Archive.test, records.example.net ,',
      STORAGE_ROOT: '/srv/archive/',
      CONVERSION_TIMEOUT_SECONDS: '30',
    });

    expect(config.partnerFeedHosts).toEqual(['feeds.partner-archive.test', 'records.example.net']);
    expect(config.storage.attachments).toBe('/srv/archive/attachments');
    expect(config.conversion.timeoutMs).toBe(30_000);
  });

  it('names every missing or malformed setting without echoing values', () => {
    const error = captureError(() =>
      loadConfig({
        ...required,
        JWT_ISSUER: 'http://identity.test',
        PSEUDONYM_KEY: 'short',
        DATABASE_URL: undefined,
      }),
    );

    expect(error).toBeInstanceOf(ConfigurationError);
    const problems = (error as ConfigurationError).problems.join('\n');
    expect(problems).toMatch(/JWT_ISSUER/);
    expect(problems).toMatch(/PSEUDONYM_KEY/);
    expect(problems).toMatch(/DATABASE_URL/);
    expect(problems).not.toMatch(/short/);
  });
});
