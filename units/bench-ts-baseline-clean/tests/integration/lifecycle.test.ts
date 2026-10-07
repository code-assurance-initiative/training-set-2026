import { EventEmitter } from 'node:events';
import request from 'supertest';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { run } from '../../src/lifecycle.js';
import { createLogger } from '../../src/logger.js';
import { createTokenIssuer, testIssuer } from '../support/test-tokens.js';

describe('run', () => {
  it('serves until the first shutdown signal', async () => {
    const tokens = await createTokenIssuer();
    const signals = new EventEmitter();
    const service = await run(
      {
        PORT: '0',
        LOG_LEVEL: 'silent',
        JWT_ISSUER: testIssuer,
        JWT_PUBLIC_KEY: tokens.publicKeyPem,
      },
      signals,
    );
    const url = `http://127.0.0.1:${String(service?.port)}`;

    await request(url).get('/health').expect(200);
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
      msg: 'Warehouse stock API failed to start',
      err: { type: 'ConfigurationError' },
    });
  });
});

describe('main', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    process.exitCode = undefined;
  });

  it('exits non-zero when the service cannot start', async () => {
    vi.stubEnv('JWT_ISSUER', '');
    vi.stubEnv('LOG_LEVEL', 'silent');

    await import('../../src/main.js');

    expect(process.exitCode).toBe(1);
  });
});
