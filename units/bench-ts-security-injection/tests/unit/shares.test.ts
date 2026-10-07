import { describe, expect, it, vi } from 'vitest';
import { readCookie } from '../../src/http/cookies.js';
import { sameSitePath } from '../../src/http/local-path.js';
import { startShareCleanup } from '../../src/shares/share-cleanup-job.js';
import { ShareGrants } from '../../src/shares/share-grants.js';
import { hashSharePassword, verifySharePassword } from '../../src/shares/share-password.js';
import { ShareStore } from '../../src/shares/share-store.js';
import { FakeSqlClient } from '../support/fake-sql.js';
import { silentLogger } from '../support/silent-logger.js';

describe('share passwords', () => {
  it('verifies the password it stored and nothing else', () => {
    const stored = hashSharePassword('board-minutes-1998');

    expect(verifySharePassword('board-minutes-1998', stored)).toBe(true);
    expect(verifySharePassword('board-minutes-1999', stored)).toBe(false);
    expect(verifySharePassword('board-minutes-1998', 'abcd')).toBe(false);
  });
});

describe('share grants', () => {
  it('allows the token it was issued for until it expires', () => {
    let clock = 0;
    const grants = new ShareGrants(1_000, () => clock);
    const grant = grants.issue('token-a');

    expect(grants.allows(grant, 'token-a')).toBe(true);
    expect(grants.allows(grant, 'token-b')).toBe(false);
    expect(grants.allows(undefined, 'token-a')).toBe(false);
    clock = 1_000;
    expect(grants.allows(grant, 'token-a')).toBe(false);
    grants.issue('token-c');
    clock = 0;
    expect(grants.allows(grant, 'token-a')).toBe(false);
  });
});

describe('share store', () => {
  it('stores links and finds only active ones', async () => {
    const db = new FakeSqlClient().willReturn().willReturn().willReturn();
    const store = new ShareStore(db);
    const now = new Date('2026-10-07T08:00:00.000Z');

    await store.create({
      token: 't',
      documentId: 'd',
      createdBy: 'archivist-4',
      passwordHash: 'h',
      expiresAt: now,
    });
    await expect(store.findActive('t', now)).resolves.toBeUndefined();
    await expect(store.purgeExpired(now)).resolves.toBe(0);
    expect(db.queries[1]?.values).toEqual(['t', now]);
  });
});

describe('share cleanup job', () => {
  it('purges expired links on every tick and survives failures', async () => {
    vi.useFakeTimers();
    try {
      const purge = vi
        .fn()
        .mockResolvedValueOnce(2)
        .mockRejectedValueOnce(new Error('db down'))
        .mockResolvedValue(0);
      const job = startShareCleanup({ purgeExpired: purge }, 1_000, silentLogger);

      await vi.advanceTimersByTimeAsync(3_000);
      job.stop();
      await vi.advanceTimersByTimeAsync(3_000);

      expect(purge).toHaveBeenCalledTimes(3);
    } finally {
      vi.useRealTimers();
    }
  });
});

describe('cookies', () => {
  it('reads one cookie by name', () => {
    expect(readCookie('a=1; share_grant=x%2By; b=2', 'share_grant')).toBe('x+y');
    expect(readCookie('a=1', 'share_grant')).toBeUndefined();
    expect(readCookie(undefined, 'share_grant')).toBeUndefined();
  });
});

describe('same-site paths', () => {
  it.each(['/searches/1', '/documents?q=board'])('accepts %s', (path) => {
    expect(sameSitePath(path)).toBe(path);
  });

  it.each([
    'https://attacker.test/',
    '//attacker.test',
    '/\\attacker.test',
    'searches',
    '/a\nb',
    42,
    '/'.padEnd(600, 'x'),
  ])('refuses %j', (path) => {
    expect(sameSitePath(path)).toBeUndefined();
  });
});
