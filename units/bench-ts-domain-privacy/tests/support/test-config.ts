import { randomBytes } from 'node:crypto';
import type { AppConfig } from '../../src/config.js';
import { testAudience, testIssuer } from './test-tokens.js';

export const emailHost = 'mail.provider.test';
export const smsHost = 'sms.provider.test';

export function testConfig(publicKeyPem = ''): AppConfig {
  return {
    environment: 'test',
    host: '127.0.0.1',
    port: 0,
    logLevel: 'silent',
    trustProxy: 'loopback',
    jwt: { issuer: testIssuer, audience: testAudience, publicKeyPem },
    databaseUrl: 'postgres://club@127.0.0.1:1/club',
    fieldEncryptionKey: randomBytes(32),
    pseudonymKey: randomBytes(32),
    email: {
      endpoint: new URL(`https://${emailHost}/v1/messages`),
      apiToken: randomBytes(16).toString('hex'),
      sender: 'classes@club.test',
    },
    sms: {
      endpoint: new URL(`https://${smsHost}/v2/sms`),
      apiToken: randomBytes(16).toString('hex'),
      sender: 'Club',
    },
    reminderLeadMinutes: 1_440,
    memberRetentionMonths: 24,
    billing: { currency: 'EUR', monthlyFeeMinor: 3_900, lateFeeMinor: 500, paymentTermDays: 14 },
    jobIntervalMs: 60_000,
  };
}
