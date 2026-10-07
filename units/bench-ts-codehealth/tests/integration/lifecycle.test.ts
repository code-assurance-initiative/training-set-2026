import { EventEmitter } from 'node:events';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import request from 'supertest';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { run } from '../../src/lifecycle.js';
import { createLogger } from '../../src/logger.js';
import { rateCard } from '../support/builders.js';
import { createTokenIssuer, testIssuer } from '../support/test-tokens.js';

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn(() => Promise.resolve(Response.json([rateCard()]))),
  );
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
  process.exitCode = undefined;
});

async function environment(): Promise<Record<string, string>> {
  const tokens = await createTokenIssuer();
  const directory = await mkdtemp(join(tmpdir(), 'service-'));
  return {
    NODE_ENV: 'test',
    PORT: '0',
    LOG_LEVEL: 'silent',
    JWT_ISSUER: testIssuer,
    JWT_PUBLIC_KEY: tokens.publicKeyPem,
    ALDER_API_KEY: 'alder-key-0123456789',
    CORVID_ACCOUNT: 'A1',
    CORVID_TOKEN: 'corvid-token-0123456789',
    WEBHOOK_SECRET: 'webhook-secret-0123456789-0123456789',
    LABEL_ARCHIVE_DIR: join(directory, 'labels'),
    MAIL_PICKUP_DIR: join(directory, 'outbox'),
  };
}

describe('run', () => {
  it('loads the rate cards and serves until the first shutdown signal', async () => {
    const signals = new EventEmitter();
    const service = await run(await environment(), signals);
    const url = `http://127.0.0.1:${String(service?.port)}`;

    await request(url).get('/health').expect(200);
    expect(fetch).toHaveBeenCalledTimes(1);
    signals.emit('SIGTERM', 'SIGTERM');
    signals.emit('SIGINT', 'SIGINT');

    await vi.waitFor(async () => {
      await expect(request(url).get('/health')).rejects.toThrow(/ECONNREFUSED/);
    });
  });

  it('logs why it cannot start and resolves to nothing', async () => {
    const lines: string[] = [];
    const bootLogger = createLogger('info', { write: (line: string) => lines.push(line) });

    const service = await run({ PORT: '0' }, new EventEmitter(), bootLogger);

    expect(service).toBeUndefined();
    expect(JSON.parse(lines[0] ?? '{}')).toMatchObject({
      level: 60,
      msg: 'Parcel rates service failed to start',
      err: { type: 'ConfigurationError' },
    });
  });
});

describe('main', () => {
  it('exits non-zero when the service cannot start', async () => {
    vi.stubEnv('JWT_ISSUER', '');
    vi.stubEnv('LOG_LEVEL', 'silent');

    await import('../../src/main.js');

    expect(process.exitCode).toBe(1);
  });
});
