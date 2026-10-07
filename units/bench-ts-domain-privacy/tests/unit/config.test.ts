import { randomBytes } from 'node:crypto';
import { describe, expect, it } from 'vitest';
import { ConfigurationError, loadConfig } from '../../src/config.js';

const valid = {
  JWT_ISSUER: 'https://identity.club.test/realms/staff',
  JWT_PUBLIC_KEY: '-----BEGIN PUBLIC KEY-----',
  DATABASE_URL: 'postgres://club@db.internal:5432/club',
  FIELD_ENCRYPTION_KEY: randomBytes(32).toString('base64'),
  PSEUDONYM_KEY: randomBytes(48).toString('base64'),
  EMAIL_API_URL: 'https://mail.provider.test/v1/messages',
  EMAIL_API_TOKEN: 'configured-at-deploy',
  EMAIL_SENDER: 'classes@club.test',
  SMS_API_URL: 'https://sms.provider.test/v2/sms',
  SMS_API_TOKEN: 'configured-at-deploy',
  SMS_SENDER: 'Club',
};

describe('loadConfig', () => {
  it('applies the documented defaults', () => {
    const config = loadConfig(valid);

    expect(config).toMatchObject({
      environment: 'production',
      port: 8080,
      reminderLeadMinutes: 1_440,
      memberRetentionMonths: 24,
      billing: { currency: 'EUR', monthlyFeeMinor: 3_900, lateFeeMinor: 500, paymentTermDays: 14 },
      jobIntervalMs: 60_000,
    });
    expect(config.fieldEncryptionKey).toHaveLength(32);
  });

  it('names every problem without echoing values', () => {
    const shortKey = randomBytes(16).toString('base64');

    const error = (() => {
      try {
        loadConfig({
          ...valid,
          FIELD_ENCRYPTION_KEY: shortKey,
          SMS_API_URL: 'http://sms.provider.test',
        });
      } catch (caught) {
        return caught;
      }
      return undefined;
    })();

    expect(error).toBeInstanceOf(ConfigurationError);
    const { problems } = error as ConfigurationError;
    expect(problems.map((problem) => problem.split(':')[0])).toEqual([
      'FIELD_ENCRYPTION_KEY',
      'SMS_API_URL',
    ]);
    expect(problems.join(' ')).not.toContain(shortKey);
  });
});
