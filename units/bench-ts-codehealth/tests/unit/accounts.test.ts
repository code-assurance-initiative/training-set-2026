import { describe, expect, it } from 'vitest';
import { CarrierAccountManager } from '../../src/application/accounts/carrier-account-manager.js';

const at = new Date('2026-10-07T10:00:00.000Z');

describe('CarrierAccountManager', () => {
  it('keeps credentials and says when they need rotating', () => {
    const accounts = new CarrierAccountManager(Buffer.from('secret'));
    accounts.setCredentials({ carrier: 'alder', accountNumber: 'A1', apiKey: 'k' }, at);
    expect(accounts.credentialsFor('alder').accountNumber).toBe('A1');
    expect(() => accounts.credentialsFor('corvid')).toThrow(/No credentials/);
    expect(accounts.needsRotation('alder', new Date('2026-12-01'))).toBe(false);
    expect(accounts.needsRotation('alder', new Date('2027-03-01'))).toBe(true);
    expect(accounts.needsRotation('corvid', at)).toBe(true);
  });

  it('limits requests per minute', () => {
    const accounts = new CarrierAccountManager(Buffer.from('secret'), 2);
    expect([accounts.tryAcquire(0), accounts.tryAcquire(10), accounts.tryAcquire(20)]).toEqual([
      true,
      true,
      false,
    ]);
    expect(accounts.remaining(30)).toBe(0);
    expect(accounts.tryAcquire(60_000)).toBe(true);
    expect(accounts.remaining(60_001)).toBe(1);
    expect(accounts.remaining(130_000)).toBe(2);
  });

  it('keeps a bounded audit trail', () => {
    const accounts = new CarrierAccountManager(Buffer.from('secret'));
    for (let i = 0; i < 1_005; i++) {
      accounts.audit(i % 2 === 0 ? 'ops' : 'api', `action ${String(i)}`, at);
    }
    expect(accounts.auditTrail()).toHaveLength(1_000);
    expect(accounts.auditTrail('ops').every((entry) => entry.actor === 'ops')).toBe(true);
  });

  it('signs and verifies webhooks, and rotates the secret', () => {
    const accounts = new CarrierAccountManager(Buffer.from('secret'));
    const signature = accounts.signWebhook('{"a":1}');
    expect(signature).toMatch(/^v1=[0-9a-f]{64}$/);
    expect(accounts.verifyWebhook('{"a":1}', signature)).toBe(true);
    expect(accounts.verifyWebhook('{"a":2}', signature)).toBe(false);
    accounts.rotateWebhookSecret(Buffer.from('other'));
    expect(accounts.verifyWebhook('{"a":1}', signature)).toBe(false);
  });
});
