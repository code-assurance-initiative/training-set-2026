import { EventEmitter } from 'node:events';
import request from 'supertest';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { run, stopOnSignals } from '../../src/lifecycle.js';
import { createLogger } from '../../src/logger.js';
import { silentLogger as silent } from '../support/silent-logger.js';
import { createCarrierSigner } from '../support/carrier-keys.js';
import { testEnvironment } from '../support/test-config.js';
import { createTokenIssuer } from '../support/test-tokens.js';

const environment = () =>
  testEnvironment(createTokenIssuer().publicKeyPem, createCarrierSigner().publicKey);

describe('run', () => {
  it('serves until the first shutdown signal', async () => {
    const signals = new EventEmitter();
    const service = await run(environment(), signals);
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
      msg: 'Depot dispatch failed to start',
      err: { type: 'ConfigurationError' },
    });
  });
});

describe('shutdown', () => {
  afterEach(() => {
    process.exitCode = undefined;
  });

  it('drops the connections still open when the grace period ends', async () => {
    const signals = new EventEmitter();
    const service = {
      port: 0,
      close: () => new Promise<void>(() => undefined),
      terminate: vi.fn(),
    };
    stopOnSignals(service, silent, signals, 10);

    signals.emit('SIGTERM', 'SIGTERM');

    await vi.waitFor(() => {
      expect(service.terminate).toHaveBeenCalledOnce();
    });
    expect(process.exitCode).toBeUndefined();
  });

  it('exits non-zero when closing fails', async () => {
    const signals = new EventEmitter();
    const service = {
      port: 0,
      close: () => Promise.reject(new Error('stuck')),
      terminate: vi.fn(),
    };
    stopOnSignals(service, silent, signals, 1_000);

    signals.emit('SIGINT', 'SIGINT');

    await vi.waitFor(() => {
      expect(process.exitCode).toBe(1);
    });
    expect(service.terminate).not.toHaveBeenCalled();
  });
});

describe('main', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    process.exitCode = undefined;
  });

  it('exits non-zero when the service cannot start', async () => {
    vi.stubEnv('TERMINAL_TOKEN_PUBLIC_KEY', '');
    vi.stubEnv('LOG_LEVEL', 'silent');

    await import('../../src/main.js');

    expect(process.exitCode).toBe(1);
  });
});
